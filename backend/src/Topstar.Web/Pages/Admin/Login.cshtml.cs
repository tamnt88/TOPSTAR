using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application;
using Topstar.Web.Pages.Public;

namespace Topstar.Web.Pages.Admin;

[AllowAnonymous]
public sealed class LoginModel(IAuthService authService) : AuthPageBase
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email quản trị.")]
        [Display(Name = "Email quản trị")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            return RedirectToSafeAdminUrl();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var result = await authService.LoginAsync(
            new LoginRequest(Input.Email, Input.Password, BuildDeviceContext()), ct);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = "Email hoặc mật khẩu quản trị không đúng.";
            return Page();
        }

        if (result.Data.Role != UserRoleType.Admin)
        {
            ErrorMessage = "Tài khoản này không có quyền truy cập trang quản trị.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Data.UserId.ToString()),
            new(ClaimTypes.Email, result.Data.Email),
            new(ClaimTypes.Name, result.Data.DisplayName),
            new(ClaimTypes.Role, result.Data.Role.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(1)
            });

        return RedirectToSafeAdminUrl();
    }

    private IActionResult RedirectToSafeAdminUrl()
        => IsSafeAdminReturnUrl(ReturnUrl) ? LocalRedirect(ReturnUrl!) : Redirect("/Admin/Dashboard");

    private bool IsSafeAdminReturnUrl(string? url)
        => !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url)
           && (url.Equals("/Admin", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("/Admin?", StringComparison.OrdinalIgnoreCase));

}
