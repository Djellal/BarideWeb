using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Correspondances
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,TenantAdmin")]
    public class EditModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(BarideDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Corresp Corresp { get; set; } = null!;

        public List<Doc> Documents { get; set; } = new();
        public SelectList CategoriesList { get; set; } = null!;
        public List<string> ExpedSuggestions { get; set; } = new();
        public List<string> ObjetSuggestions { get; set; } = new();
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
            await LoadFormData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<IFormFile> newFiles, string? scannedFilesJson)
        {
            var existing = await _context.Correspondances.FindAsync(Corresp.Cid);
            if (existing == null) return NotFound();

            existing.Num = Corresp.Num;
            existing.NumInterne = Corresp.NumInterne;
            existing.DateCorresp = DateTime.SpecifyKind(Corresp.DateCorresp, DateTimeKind.Utc);
            existing.DateArrivDepart = DateTime.SpecifyKind(Corresp.DateArrivDepart, DateTimeKind.Utc);
            existing.Expediteur = Corresp.Expediteur;
            existing.Objet = Corresp.Objet;
            existing.Observation = Corresp.Observation;
            existing.CatId = Corresp.CatId;

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            int pageNum = await _context.Documents.CountAsync(d => d.Cid == Corresp.Cid) + 1;

            // Handle new file uploads
            foreach (var file in newFiles)
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
            return RedirectToPage("/Correspondances/Edit", new { id = Corresp.Cid });
        }

        private class ScannedFileDto
        {
            public string Name { get; set; } = "";
            public string DataUrl { get; set; } = "";
        }

        public async Task<IActionResult> OnPostDeleteDocAsync(Guid docId)
        {
            var doc = await _context.Documents.FindAsync(docId);
            if (doc != null)
            {
                if (!string.IsNullOrEmpty(doc.FileName))
                {
                    var filePath = Path.Combine(_env.WebRootPath, "uploads", doc.FileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Correspondances/Edit", new { id = Corresp.Cid });
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

            var scannerParams = await _context.Parameters
                .Where(p => p.Key == "ScannerDpi" || p.Key == "ScannerPixelMode" || p.Key == "ScannerImageFormat")
                .ToListAsync();
            ViewData["ScannerDpi"] = scannerParams.FirstOrDefault(p => p.Key == "ScannerDpi")?.Value ?? "200";
            ViewData["ScannerPixelMode"] = scannerParams.FirstOrDefault(p => p.Key == "ScannerPixelMode")?.Value ?? "Grayscale";
            ViewData["ScannerImageFormat"] = scannerParams.FirstOrDefault(p => p.Key == "ScannerImageFormat")?.Value ?? "PDF";
        }
    }
}
