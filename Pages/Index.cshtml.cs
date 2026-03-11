using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly IWebHostEnvironment _env;

        public IndexModel(BarideDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public List<Corresp> Correspondances { get; set; } = new();
        public List<Categorie> Categories { get; set; } = new();
        public TypeCorresp CurrentType { get; set; } = TypeCorresp.Entrant_Interne;
        public string ExpedLabel { get; set; } = "المرسل";
        public string? SearchText { get; set; }
        public bool SearchAll { get; set; }

        public ViewMode CorrespViewMode { get; set; } = ViewMode.NewTab;

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

            // Load view mode parameter
            var viewModeParam = await _context.Parameters
                .FirstOrDefaultAsync(p => p.Key == "CorrespViewMode");
            if (viewModeParam != null && int.TryParse(viewModeParam.Value, out var vm))
            {
                CorrespViewMode = (ViewMode)vm;
            }
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
    }
}
