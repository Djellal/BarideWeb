using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Tenants
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(BarideDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<TenantViewModel> Tenants { get; set; } = new();
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync(string? message = null)
        {
            StatusMessage = message;
            await LoadTenantsAsync();
        }

        public async Task<IActionResult> OnPostAddAsync(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var tenant = new Tenant { Name = name.Trim() };
                _context.Tenants.Add(tenant);

                // Seed default categories for the new tenant
                var defaultCategories = new[]
                {
                    "المراسلات", "المحاضر", "التقارير", "عروض الحال", "سندات الطلب",
                    "الدعوات", "الاعلانات", "المذكرات", "الشهادات", "المقررات", "التهاني"
                };
                foreach (var cat in defaultCategories)
                {
                    _context.Categories.Add(new Categorie
                    {
                        Designation = cat,
                        TenantId = tenant.TenantId
                    });
                }

                await _context.SaveChangesAsync();
                return RedirectToPage(new { message = $"تم إضافة المؤسسة: {name}" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(Guid tenantId, string name)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant != null && !string.IsNullOrWhiteSpace(name))
            {
                tenant.Name = name.Trim();
                await _context.SaveChangesAsync();
                return RedirectToPage(new { message = $"تم تعديل المؤسسة" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant != null)
            {
                // Check if tenant has users
                var hasUsers = await _userManager.Users.AnyAsync(u => u.TenantId == tenantId);
                if (hasUsers)
                {
                    return RedirectToPage(new { message = "لا يمكن حذف مؤسسة بها مستخدمون" });
                }

                // Remove tenant categories and correspondances
                var corresps = await _context.Correspondances.IgnoreQueryFilters()
                    .Where(c => c.TenantId == tenantId).ToListAsync();
                var docs = await _context.Documents
                    .Where(d => corresps.Select(c => c.Cid).Contains(d.Cid)).ToListAsync();
                _context.Documents.RemoveRange(docs);
                _context.Correspondances.RemoveRange(corresps);

                var cats = await _context.Categories.IgnoreQueryFilters()
                    .Where(c => c.TenantId == tenantId).ToListAsync();
                _context.Categories.RemoveRange(cats);

                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { message = "تم حذف المؤسسة" });
            }
            return RedirectToPage();
        }

        private async Task LoadTenantsAsync()
        {
            var tenants = await _context.Tenants.ToListAsync();
            Tenants = new List<TenantViewModel>();
            foreach (var t in tenants)
            {
                var userCount = await _userManager.Users.CountAsync(u => u.TenantId == t.TenantId);
                var correspCount = await _context.Correspondances.IgnoreQueryFilters()
                    .CountAsync(c => c.TenantId == t.TenantId);
                Tenants.Add(new TenantViewModel
                {
                    TenantId = t.TenantId,
                    Name = t.Name,
                    UserCount = userCount,
                    CorrespCount = correspCount
                });
            }
        }
    }

    public class TenantViewModel
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = "";
        public int UserCount { get; set; }
        public int CorrespCount { get; set; }
    }
}
