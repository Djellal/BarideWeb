using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Pages.Account.Users
{
    [Authorize(Roles = "Admin,TenantAdmin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly BarideDbContext _context;

        public IndexModel(UserManager<AppUser> userManager, BarideDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<Tenant> Tenants { get; set; } = new();
        public string? StatusMessage { get; set; }
        public bool IsGlobalAdmin { get; set; }

        public async Task OnGetAsync(string? message = null)
        {
            StatusMessage = message;
            IsGlobalAdmin = User.IsInRole("Admin");
            Tenants = await _context.Tenants.ToListAsync();
            await LoadUsersAsync();
        }

        public async Task<IActionResult> OnPostAddAsync(string username, string fullName, string password, string role, Guid? tenantId)
        {
            var isGlobalAdmin = User.IsInRole("Admin");

            // TenantAdmin can only create users in their own tenant
            if (!isGlobalAdmin)
            {
                var currentTenantId = GetCurrentTenantId();
                tenantId = currentTenantId;
                if (role == "Admin") role = "User"; // TenantAdmin can't create global admins
            }

            var user = new AppUser
            {
                UserName = username,
                FullName = fullName,
                EmailConfirmed = true,
                TenantId = tenantId
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return RedirectToPage(new { message = $"تم إضافة المستخدم {username}" });
            }

            StatusMessage = "خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
            IsGlobalAdmin = isGlobalAdmin;
            Tenants = await _context.Tenants.ToListAsync();
            await LoadUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.UserName != "admin")
            {
                // TenantAdmin can only delete users in their tenant
                if (!User.IsInRole("Admin"))
                {
                    var currentTenantId = GetCurrentTenantId();
                    if (user.TenantId != currentTenantId)
                        return RedirectToPage();
                }

                await _userManager.DeleteAsync(user);
                return RedirectToPage(new { message = $"تم حذف المستخدم {user.UserName}" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(string userId, string fullName, string role, Guid? tenantId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.UserName == "admin")
                return RedirectToPage();

            var isGlobalAdmin = User.IsInRole("Admin");

            // TenantAdmin can only edit users in their own tenant
            if (!isGlobalAdmin)
            {
                var currentTenantId = GetCurrentTenantId();
                if (user.TenantId != currentTenantId)
                    return RedirectToPage();
                tenantId = currentTenantId; // keep same tenant
                if (role == "Admin") role = "User"; // can't promote to global admin
            }

            user.FullName = fullName;
            if (isGlobalAdmin)
                user.TenantId = tenantId;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "خطأ: " + string.Join(", ", updateResult.Errors.Select(e => e.Description));
                IsGlobalAdmin = isGlobalAdmin;
                Tenants = await _context.Tenants.ToListAsync();
                await LoadUsersAsync();
                return Page();
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToPage(new { message = $"تم تعديل المستخدم {user.UserName}" });
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // TenantAdmin can only reset passwords for users in their tenant
                if (!User.IsInRole("Admin"))
                {
                    var currentTenantId = GetCurrentTenantId();
                    if (user.TenantId != currentTenantId)
                        return RedirectToPage();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (result.Succeeded)
                    return RedirectToPage(new { message = $"تم تغيير كلمة مرور {user.UserName}" });

                StatusMessage = "خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
                IsGlobalAdmin = User.IsInRole("Admin");
                Tenants = await _context.Tenants.ToListAsync();
                await LoadUsersAsync();
                return Page();
            }
            return RedirectToPage();
        }

        private async Task LoadUsersAsync()
        {
            var isGlobalAdmin = User.IsInRole("Admin");
            var currentTenantId = GetCurrentTenantId();

            var allUsers = isGlobalAdmin
                ? await _userManager.Users.ToListAsync()
                : await _userManager.Users.Where(u => u.TenantId == currentTenantId).ToListAsync();

            Users = new List<UserViewModel>();
            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var tenantName = "";
                if (u.TenantId.HasValue)
                {
                    var tenant = await _context.Tenants.FindAsync(u.TenantId.Value);
                    tenantName = tenant?.Name ?? "";
                }

                Users.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = u.FullName,
                    Role = roles.FirstOrDefault() ?? "User",
                    TenantName = tenantName,
                    TenantId = u.TenantId
                });
            }
        }

        private Guid? GetCurrentTenantId()
        {
            var claim = User.FindFirst("TenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var tid))
                return tid;
            return null;
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string TenantName { get; set; } = "";
        public Guid? TenantId { get; set; }
    }
}
