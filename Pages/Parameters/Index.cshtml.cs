using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;
using BarideWeb.Services;

namespace BarideWeb.Pages.Parameters
{
    [Authorize(Roles = "Admin,TenantAdmin")]
    public class IndexModel : PageModel
    {
        private readonly BarideDbContext _context;
        private readonly ITenantService _tenantService;

        public IndexModel(BarideDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public List<TenantParametersViewModel> TenantParams { get; set; } = new();
        public string? StatusMessage { get; set; }
        public bool IsAdmin => User.IsInRole("Admin");

        public async Task OnGetAsync(string? message = null)
        {
            StatusMessage = message;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync(Guid tenantId, Dictionary<string, string> param)
        {
            // TenantAdmin can only save their own tenant
            if (!IsAdmin)
            {
                var currentTenantId = _tenantService.GetCurrentTenantId();
                if (currentTenantId == null || currentTenantId != tenantId)
                    return Forbid();
            }

            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return RedirectToPage();

            foreach (var kvp in param)
            {
                var existing = await _context.Parameters.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == kvp.Key);

                if (existing != null)
                {
                    existing.Value = kvp.Value;
                }
                else
                {
                    _context.Parameters.Add(new AppParameter
                    {
                        Key = kvp.Key,
                        Value = kvp.Value,
                        TenantId = tenantId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { message = $"تم حفظ إعدادات المؤسسة: {tenant.Name}" });
        }

        private async Task LoadAsync()
        {
            List<Tenant> tenants;

            if (IsAdmin)
            {
                tenants = await _context.Tenants.ToListAsync();
            }
            else
            {
                var currentTenantId = _tenantService.GetCurrentTenantId();
                tenants = currentTenantId.HasValue
                    ? await _context.Tenants.Where(t => t.TenantId == currentTenantId.Value).ToListAsync()
                    : new List<Tenant>();
            }

            TenantParams = new List<TenantParametersViewModel>();

            foreach (var t in tenants)
            {
                var parameters = await _context.Parameters.IgnoreQueryFilters()
                    .Where(p => p.TenantId == t.TenantId)
                    .ToListAsync();

                // Ensure default parameters exist
                var defaults = new Dictionary<string, (string Value, string Description)>
                {
                    ["CorrespViewMode"] = ("0", "طريقة عرض المراسلة: 0=في صفحة جديدة، 1=في نافذة منبثقة، 2=في نفس الصفحة"),
                    ["ScannerDpi"] = ("200", "الدقة الافتراضية للماسح الضوئي (DPI): 100, 150, 200, 300, 600"),
                    ["ScannerPixelMode"] = ("Grayscale", "وضع اللون الافتراضي للماسح الضوئي: Color, Grayscale"),
                    ["ScannerImageFormat"] = ("PDF", "صيغة الصورة الافتراضية للماسح الضوئي: JPG, PNG, PDF"),
                };

                bool added = false;
                foreach (var d in defaults)
                {
                    if (!parameters.Any(p => p.Key == d.Key))
                    {
                        var newParam = new AppParameter
                        {
                            Key = d.Key,
                            Value = d.Value.Value,
                            Description = d.Value.Description,
                            TenantId = t.TenantId
                        };
                        _context.Parameters.Add(newParam);
                        parameters.Add(newParam);
                        added = true;
                    }
                }
                if (added) await _context.SaveChangesAsync();

                TenantParams.Add(new TenantParametersViewModel
                {
                    TenantId = t.TenantId,
                    TenantName = t.Name,
                    Parameters = parameters
                });
            }
        }
    }

    public class TenantParametersViewModel
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public List<AppParameter> Parameters { get; set; } = new();
    }
}
