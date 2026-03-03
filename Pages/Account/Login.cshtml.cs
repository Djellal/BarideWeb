using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarideWeb.Models;

namespace BarideWeb.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;

        public LoginModel(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public bool RememberMe { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var result = await _signInManager.PasswordSignInAsync(Username, Password, RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return Page();
        }
    }
}
