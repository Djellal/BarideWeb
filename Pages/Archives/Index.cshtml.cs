using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Archives
{
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly IConfiguration _config;

        public IndexModel(BarideDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public string CurrentArchive { get; set; } = "baride.db";
        public string DbPath { get; set; } = "";
        public Dictionary<string, int> Stats { get; set; } = new();
        public string? Message { get; set; }

        public async Task OnGetAsync(string? msg = null)
        {
            Message = msg;
            DbPath = Path.Combine(Directory.GetCurrentDirectory(), "baride.db");
            CurrentArchive = "baride.db";

            Stats["وارد داخلي"] = await _context.Correspondances.CountAsync(c => c.Type == TypeCorresp.Entrant_Interne);
            Stats["وارد خارجي"] = await _context.Correspondances.CountAsync(c => c.Type == TypeCorresp.Entrant_Externe);
            Stats["صادر داخلي"] = await _context.Correspondances.CountAsync(c => c.Type == TypeCorresp.Sotant_Interne);
            Stats["صادر خارجي"] = await _context.Correspondances.CountAsync(c => c.Type == TypeCorresp.Sortant_Externe);
            Stats["متفرقات"] = await _context.Correspondances.CountAsync(c => c.Type == TypeCorresp.Divers);
        }

        public IActionResult OnGetDownload()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "baride.db");
            if (!System.IO.File.Exists(dbPath))
                return NotFound();

            var bytes = System.IO.File.ReadAllBytes(dbPath);
            return File(bytes, "application/octet-stream", $"baride_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        }
    }
}
