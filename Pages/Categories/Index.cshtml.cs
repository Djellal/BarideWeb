using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;

        public IndexModel(BarideDbContext context)
        {
            _context = context;
        }

        public List<Categorie> Categories { get; set; } = new();
        public string? SearchText { get; set; }
        public string? Message { get; set; }

        public async Task OnGetAsync(string? search = null, string? msg = null)
        {
            SearchText = search;
            Message = msg;

            var query = _context.Categories.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Designation.Contains(search));

            Categories = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync(string designation)
        {
            if (!string.IsNullOrWhiteSpace(designation))
            {
                var cat = new Categorie { Designation = designation.Trim() };
                _context.Categories.Add(cat);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { msg = "تم إضافة الصنف بنجاح" });
        }

        public async Task<IActionResult> OnPostEditAsync(Guid catId, string designation)
        {
            var cat = await _context.Categories.FindAsync(catId);
            if (cat != null && !string.IsNullOrWhiteSpace(designation))
            {
                cat.Designation = designation.Trim();
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { msg = "تم تعديل الصنف بنجاح" });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid catId)
        {
            var cat = await _context.Categories.FindAsync(catId);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { msg = "تم حذف الصنف بنجاح" });
        }
    }
}
