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
        public bool ShowAll { get; set; }

        public async Task OnGetAsync(int type = 0)
        {
            ShowAll = type < 0;
            var tp = (TypeCorresp)type;

            ReportTitle = ShowAll ? "قائمة المراسلات" : tp switch
            {
                TypeCorresp.Entrant_Interne => "قائمة البريد الوارد الداخلي",
                TypeCorresp.Entrant_Externe => "قائمة البريد الوارد الخارجي",
                TypeCorresp.Sotant_Interne => "قائمة البريد الصادر الداخلي",
                TypeCorresp.Sortant_Externe => "قائمة البريد الصادر الخارجي",
                TypeCorresp.Divers => "قائمة المتفرقات",
                _ => "قائمة المراسلات"
            };

            ExpedLabel = ShowAll
                ? "المرسل/المرسل إليه"
                : tp == TypeCorresp.Entrant_Interne || tp == TypeCorresp.Entrant_Externe
                    ? "المرسل" : "المرسل إليه";

            IQueryable<Corresp> query = _context.Correspondances.Include(c => c.Categ);

            if (!ShowAll)
                query = query.Where(c => c.Type == tp);

            Correspondances = await query
                .OrderByDescending(c => c.DateArrivDepart)
                .ToListAsync();
        }

        public static string TypeArabicLabel(TypeCorresp? tp) => tp switch
        {
            TypeCorresp.Entrant_Interne => "وارد داخلي",
            TypeCorresp.Entrant_Externe => "وارد خارجي",
            TypeCorresp.Sotant_Interne => "صادر داخلي",
            TypeCorresp.Sortant_Externe => "صادر خارجي",
            TypeCorresp.Divers => "متفرقات",
            _ => ""
        };
    }
}
