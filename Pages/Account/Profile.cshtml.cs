using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarideWeb.Models;

namespace BarideWeb.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ProfileModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? TenantName { get; set; }
        public string Role { get; set; } = "";
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? message = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            StatusMessage = message;
            await LoadUserInfo(user);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync(string fullName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            user.FullName = fullName;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToPage(new { message = "تم تحديث المعلومات بنجاح" });

            StatusMessage = "خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
            await LoadUserInfo(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToPage(new { message = "تم تغيير كلمة المرور بنجاح" });
            }

            StatusMessage = "خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
            await LoadUserInfo(user);
            return Page();
        }

        private async Task LoadUserInfo(AppUser user)
        {
            UserName = user.UserName ?? "";
            FullName = user.FullName;
            var roles = await _userManager.GetRolesAsync(user);
            Role = roles.FirstOrDefault() ?? "User";

            if (user.TenantId.HasValue)
            {
                TenantName = User.FindFirst("TenantName")?.Value ?? "";
            }
        }
    }
}
