using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using BarideWeb.Services;

namespace BarideWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public IndexModel(BarideDbContext context, IWebHostEnvironment env, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _emailService = emailService;
        }

        public List<Corresp> Correspondances { get; set; } = new();
        public List<Categorie> Categories { get; set; } = new();
        public TypeCorresp CurrentType { get; set; } = TypeCorresp.Entrant_Interne;
        public string ExpedLabel { get; set; } = "المرسل";
        public string? SearchText { get; set; }
        public bool SearchAll { get; set; }

        public ViewMode CorrespViewMode { get; set; } = ViewMode.NewTab;
        public Dictionary<Guid, int> TransfertCounts { get; set; } = new();
        public List<AppUser> Users { get; set; } = new();

        // Filter properties
        public Guid? FilterCatId { get; set; }
        public string? FilterExped { get; set; }
        public string? FilterObjet { get; set; }
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }

        public async Task OnGetAsync(int type = 0, string? search = null,
            Guid? catId = null, string? exped = null, string? objet = null,
            DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            CurrentType = (TypeCorresp)type;
            SearchText = search;
            FilterCatId = catId;
            FilterExped = exped;
            FilterObjet = objet;
            FilterDateFrom = dateFrom;
            FilterDateTo = dateTo;

            ExpedLabel = CurrentType == TypeCorresp.Entrant_Interne || CurrentType == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";

            Categories = await _context.Categories.ToListAsync();

            IQueryable<Corresp> query = _context.Correspondances
                .Include(c => c.Categ);

            // Text search across all types, or filter by current type
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Num.Contains(search) ||
                    c.NumInterne.Contains(search) ||
                    c.Expediteur.Contains(search) ||
                    c.Objet.Contains(search) ||
                    (c.Observation != null && c.Observation.Contains(search)));
                SearchAll = true;
            }
            else
            {
                query = query.Where(c => c.Type == CurrentType);
            }

            // Advanced filters
            if (catId.HasValue)
                query = query.Where(c => c.CatId == catId.Value);

            if (!string.IsNullOrWhiteSpace(exped))
                query = query.Where(c => c.Expediteur.Contains(exped));

            if (!string.IsNullOrWhiteSpace(objet))
                query = query.Where(c => c.Objet.Contains(objet));

            if (dateFrom.HasValue)
                query = query.Where(c => c.DateCorresp >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(c => c.DateCorresp <= dateTo.Value.AddDays(1));

            Correspondances = await query.OrderByDescending(c => c.DateArrivDepart).ToListAsync();

            var cids = Correspondances.Select(c => c.Cid).ToList();
            TransfertCounts = await _context.Transferts
                .Where(t => cids.Contains(t.Cid))
                .GroupBy(t => t.Cid)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            // Load view mode parameter
            var viewModeParam = await _context.Parameters
                .FirstOrDefaultAsync(p => p.Key == "CorrespViewMode");
            if (viewModeParam != null && int.TryParse(viewModeParam.Value, out var vm))
            {
                CorrespViewMode = (ViewMode)vm;
            }

            await LoadUsers();
        }

        public async Task<IActionResult> OnGetViewPartialAsync(Guid id)
        {
            var corresp = await _context.Correspondances
                .Include(c => c.Categ)
                .FirstOrDefaultAsync(c => c.Cid == id);

            if (corresp == null) return NotFound();

            var documents = await _context.Documents.Where(d => d.Cid == id).ToListAsync();

            var typeLabel = corresp.Type switch
            {
                TypeCorresp.Entrant_Interne => "وارد داخلي",
                TypeCorresp.Entrant_Externe => "وارد خارجي",
                TypeCorresp.Sotant_Interne => "صادر داخلي",
                TypeCorresp.Sortant_Externe => "صادر خارجي",
                TypeCorresp.Divers => "متفرقات",
                _ => ""
            };

            var expedLabel = corresp.Type == TypeCorresp.Entrant_Interne || corresp.Type == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";

            return Partial("_CorrespView", new CorrespViewData
            {
                Corresp = corresp,
                Documents = documents,
                TypeLabel = typeLabel,
                ExpedLabel = expedLabel,
                IsModal = true
            });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, int type)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("TenantAdmin"))
                return Forbid();

            var corresp = await _context.Correspondances.FindAsync(id);
            if (corresp != null)
            {
                var docs = await _context.Documents.Where(d => d.Cid == id).ToListAsync();
                foreach (var doc in docs)
                {
                    if (!string.IsNullOrEmpty(doc.FileName))
                    {
                        var filePath = Path.Combine(_env.WebRootPath, "uploads", doc.FileName);
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                }
                _context.Documents.RemoveRange(docs);
                _context.Correspondances.Remove(corresp);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Index", new { type });
        }

        public async Task<IActionResult> OnPostTransferAsync(Guid cid, List<string> receiverIds, string? note)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var receiverId in receiverIds)
            {
                _context.Transferts.Add(new Transfert
                {
                    Cid = cid,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Note = note,
                    DateTransfert = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/Index", new { type = (int)(corresp.Type ?? TypeCorresp.Entrant_Interne) });
        }

        public async Task<IActionResult> OnPostShareAsync(Guid cid, string? contactsJson)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            if (string.IsNullOrWhiteSpace(contactsJson))
                return RedirectToPage("/Index", new { type = (int)(corresp.Type ?? TypeCorresp.Entrant_Interne) });

            var contacts = JsonSerializer.Deserialize<List<ShareContactDto>>(contactsJson);
            if (contacts == null || contacts.Count == 0)
                return RedirectToPage("/Index", new { type = (int)(corresp.Type ?? TypeCorresp.Entrant_Interne) });

            var viewUrl = $"{Request.Scheme}://{Request.Host}/Correspondances/View/{cid}";
            var subject = $"مراسلة رقم {corresp.Num} - {corresp.Objet}";
            var body = $@"
                <div dir='rtl' style='font-family: Cairo, sans-serif;'>
                    <h3>مراسلة رقم {corresp.Num}</h3>
                    <p><strong>الموضوع:</strong> {corresp.Objet}</p>
                    <p><strong>المرسل:</strong> {corresp.Expediteur}</p>
                    <p><a href='{viewUrl}'>اضغط هنا لعرض المراسلة</a></p>
                </div>";

            foreach (var dto in contacts)
            {
                if (string.IsNullOrWhiteSpace(dto.Email)) continue;

                var existing = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Email == dto.Email);

                if (existing == null)
                {
                    var newContact = new Contact
                    {
                        Name = dto.Name ?? dto.Email,
                        Email = dto.Email,
                        Phone = dto.Phone ?? ""
                    };
                    _context.Contacts.Add(newContact);
                }

                await _emailService.SendEmailAsync(dto.Email, dto.Name ?? dto.Email, subject, body);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Index", new { type = (int)(corresp.Type ?? TypeCorresp.Entrant_Interne) });
        }

        private class ShareContactDto
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
        }

        private async Task LoadUsers()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantClaim = User.FindFirst("TenantId");
            Guid? tenantId = tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tid) ? tid : null;

            if (tenantId.HasValue)
            {
                Users = await _userManager.Users
                    .Where(u => u.TenantId == tenantId && u.Id != currentUserId)
                    .ToListAsync();
            }
            else
            {
                Users = await _userManager.Users
                    .Where(u => u.Id != currentUserId)
                    .ToListAsync();
            }
        }
    }
}
