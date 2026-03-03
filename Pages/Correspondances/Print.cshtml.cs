using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Correspondances
{
    public class PrintModel : PageModel
    {
        private readonly BarideDbContext _context;

        public PrintModel(BarideDbContext context)
        {
            _context = context;
        }

        public List<Corresp> Correspondances { get; set; } = new();
        public string ReportTitle { get; set; } = "";
        public string ExpedLabel { get; set; } = "المرسل";

        public async Task OnGetAsync(int type = 0)
        {
            var tp = (TypeCorresp)type;

            ReportTitle = tp switch
            {
                TypeCorresp.Entrant_Interne => "قائمة البريد الوارد الداخلي",
                TypeCorresp.Entrant_Externe => "قائمة البريد الوارد الخارجي",
                TypeCorresp.Sotant_Interne => "قائمة البريد الصادر الداخلي",
                TypeCorresp.Sortant_Externe => "قائمة البريد الصادر الخارجي",
                TypeCorresp.Divers => "قائمة المتفرقات",
                _ => "قائمة المراسلات"
            };

            ExpedLabel = tp == TypeCorresp.Entrant_Interne || tp == TypeCorresp.Entrant_Externe
                ? "المرسل" : "المرسل إليه";

            Correspondances = await _context.Correspondances
                .Include(c => c.Categ)
                .Where(c => c.Type == tp)
                .OrderByDescending(c => c.DateArrivDepart)
                .ToListAsync();
        }
    }
}
