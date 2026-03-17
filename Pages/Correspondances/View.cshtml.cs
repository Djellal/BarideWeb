using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using BarideWeb.Services;

namespace BarideWeb.Pages.Correspondances
{
    public class ViewModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public ViewModel(BarideDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public Corresp Corresp { get; set; } = null!;
        public List<Doc> Documents { get; set; } = new();
        public List<AppUser> Users { get; set; } = new();
        public string TypeLabel { get; set; } = "";
        public string ExpedLabel { get; set; } = "المرسل";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var corresp = await _context.Correspondances
                .Include(c => c.Categ)
                .FirstOrDefaultAsync(c => c.Cid == id);

            if (corresp == null) return NotFound();

            Corresp = corresp;
            Documents = await _context.Documents.Where(d => d.Cid == id).ToListAsync();
            await LoadUsers();

            TypeLabel = Corresp.Type switch
            {
                TypeCorresp.Entrant_Interne => "وارد داخلي",
                TypeCorresp.Entrant_Externe => "وارد خارجي",
                TypeCorresp.Sotant_Interne => "صادر داخلي",
                TypeCorresp.Sortant_Externe => "صادر خارجي",
                TypeCorresp.Divers => "متفرقات",
                _ => ""
            };

            ExpedLabel = Corresp.Type == TypeCorresp.Entrant_Interne || Corresp.Type == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";

            return Page();
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

            return RedirectToPage(new { id = cid });
        }

        public async Task<IActionResult> OnPostShareAsync(Guid cid, string? contactsJson)
        {
            var corresp = await _context.Correspondances.FindAsync(cid);
            if (corresp == null) return NotFound();

            if (string.IsNullOrWhiteSpace(contactsJson))
                return RedirectToPage(new { id = cid });

            var contacts = JsonSerializer.Deserialize<List<ShareContactDto>>(contactsJson);
            if (contacts == null || contacts.Count == 0)
                return RedirectToPage(new { id = cid });

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

                // Find or create contact
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
            return RedirectToPage(new { id = cid });
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
