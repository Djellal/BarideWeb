using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Statistics
{
    [Authorize(Roles = "Admin,TenantAdmin")]
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;

        public IndexModel(BarideDbContext context)
        {
            _context = context;
        }

        public int TotalCorrespondances { get; set; }
        public int TotalEntrantInterne { get; set; }
        public int TotalEntrantExterne { get; set; }
        public int TotalSortantInterne { get; set; }
        public int TotalSortantExterne { get; set; }
        public int TotalDivers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalCategories { get; set; }

        // Monthly stats for current year
        public int[] MonthlyEntrant { get; set; } = new int[12];
        public int[] MonthlySortant { get; set; } = new int[12];

        // Top categories
        public List<CategoryStat> TopCategories { get; set; } = new();

        // Top expediteurs
        public List<ExpedStat> TopExpediteurs { get; set; } = new();

        public int CurrentYear { get; set; }

        public async Task OnGetAsync(int? year = null)
        {
            CurrentYear = year ?? DateTime.Now.Year;

            var corresps = _context.Correspondances.AsQueryable();

            TotalCorrespondances = await corresps.CountAsync();
            TotalEntrantInterne = await corresps.CountAsync(c => c.Type == TypeCorresp.Entrant_Interne);
            TotalEntrantExterne = await corresps.CountAsync(c => c.Type == TypeCorresp.Entrant_Externe);
            TotalSortantInterne = await corresps.CountAsync(c => c.Type == TypeCorresp.Sotant_Interne);
            TotalSortantExterne = await corresps.CountAsync(c => c.Type == TypeCorresp.Sortant_Externe);
            TotalDivers = await corresps.CountAsync(c => c.Type == TypeCorresp.Divers);
            TotalDocuments = await _context.Documents.CountAsync();
            TotalCategories = await _context.Categories.CountAsync();

            // Monthly breakdown for selected year
            var yearCorresps = await corresps
                .Where(c => c.DateCorresp.Year == CurrentYear)
                .Select(c => new { c.Type, c.DateCorresp.Month })
                .ToListAsync();

            foreach (var c in yearCorresps)
            {
                if (c.Type == TypeCorresp.Entrant_Interne || c.Type == TypeCorresp.Entrant_Externe)
                    MonthlyEntrant[c.Month - 1]++;
                else if (c.Type == TypeCorresp.Sotant_Interne || c.Type == TypeCorresp.Sortant_Externe)
                    MonthlySortant[c.Month - 1]++;
            }

            // Top categories
            TopCategories = await corresps
                .Where(c => c.Categ != null)
                .GroupBy(c => c.Categ!.Designation)
                .Select(g => new CategoryStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Top expediteurs
            TopExpediteurs = await corresps
                .GroupBy(c => c.Expediteur)
                .Select(g => new ExpedStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();
        }

        public class CategoryStat
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }

        public class ExpedStat
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
