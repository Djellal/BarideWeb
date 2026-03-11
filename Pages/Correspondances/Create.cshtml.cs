using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Correspondances
{
    public class CreateModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public CreateModel(BarideDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

        [BindProperty]
        public Corresp Corresp { get; set; } = new();

        public SelectList CategoriesList { get; set; } = null!;
        public List<string> ExpedSuggestions { get; set; } = new();
        public List<string> ObjetSuggestions { get; set; } = new();
        public string TypeLabel { get; set; } = "";
        public string ExpedLabel { get; set; } = "المرسل";

        public async Task OnGetAsync(int type = 0)
        {
            var tp = (TypeCorresp)type;
            Corresp.Type = tp;
            Corresp.Cid = Guid.NewGuid();

            int numLen = _config.GetValue<int>("BarideSettings:NumLength", 4);
            Corresp.Num = await GenerateNum(tp, numLen);
            Corresp.NumInterne = await GenerateNumInterne(tp, numLen);

            await LoadFormData();
        }

        public async Task<IActionResult> OnPostAsync(List<IFormFile> files, string? scannedFilesJson)
        {
            if (Corresp.CatId == Guid.Empty)
            {
                ModelState.AddModelError("Corresp.CatId", "يجب اختيار صنف المراسلة");
                await LoadFormData();
                return Page();
            }

            _context.Correspondances.Add(Corresp);

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            int pageNum = 1;

            // Handle file uploads
            foreach (var file in files)
            {
                if (file.Length > 21_000_000) continue;

                var doc = new Doc
                {
                    DocId = Guid.NewGuid(),
                    Cid = Corresp.Cid,
                    Designation = "الصفحة " + pageNum++
                };

                var ext = Path.GetExtension(file.FileName);
                doc.FileName = doc.DocId + ext;

                var filePath = Path.Combine(uploadsDir, doc.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.Documents.Add(doc);
            }

            // Handle scanned files from JSPrintManager
            if (!string.IsNullOrEmpty(scannedFilesJson))
            {
                var scannedFiles = JsonSerializer.Deserialize<List<ScannedFileDto>>(scannedFilesJson);
                if (scannedFiles != null)
                {
                    foreach (var sf in scannedFiles)
                    {
                        var doc = new Doc
                        {
                            DocId = Guid.NewGuid(),
                            Cid = Corresp.Cid,
                            Designation = "الصفحة " + pageNum++
                        };

                        var ext = Path.GetExtension(sf.Name);
                        doc.FileName = doc.DocId + ext;

                        var bytes = Convert.FromBase64String(sf.DataUrl.Substring(sf.DataUrl.IndexOf(',') + 1));
                        var filePath = Path.Combine(uploadsDir, doc.FileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                        _context.Documents.Add(doc);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Index", new { type = (int)Corresp.Type! });
        }

        private class ScannedFileDto
        {
            public string Name { get; set; } = "";
            public string DataUrl { get; set; } = "";
        }

        private async Task LoadFormData()
        {
            var categories = await _context.Categories.ToListAsync();
            CategoriesList = new SelectList(categories, "CatId", "Designation");
            ExpedSuggestions = await _context.Correspondances.Select(c => c.Expediteur).Distinct().ToListAsync();
            ObjetSuggestions = await _context.Correspondances.Select(c => c.Objet).Distinct().ToListAsync();

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
        }

        private async Task<string> GenerateNum(TypeCorresp tp, int numLen)
        {
            int count = await _context.Correspondances.CountAsync(c => c.Type == tp) + 1;
            string num = count.ToString("D" + numLen);
            while (await _context.Correspondances.AnyAsync(c => c.Type == tp && c.Num == num))
            {
                num = (++count).ToString("D" + numLen);
            }
            return count.ToString("D" + numLen);
        }

        private async Task<string> GenerateNumInterne(TypeCorresp tp, int numLen)
        {
            int count = await _context.Correspondances.CountAsync(c => c.Type == tp) + 1;
            string num = count.ToString("D" + numLen);
            while (await _context.Correspondances.AnyAsync(c => c.Type == tp && c.NumInterne == num))
            {
                num = (++count).ToString("D" + numLen);
            }
            return count.ToString("D" + numLen);
        }
    }
}
