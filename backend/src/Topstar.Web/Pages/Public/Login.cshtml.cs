using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class LoginModel(IAuthService authService) : AuthPageBase
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Display(Name = "Email hoặc số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await authService.LoginAsync(
            new LoginRequest(Input.EmailOrPhone, Input.Password, BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var authProps = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(1)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        return Redirect(user.Role switch
        {
            UserRoleType.Admin => "/Admin/Dashboard",
            UserRoleType.Brand => "/Brand/Dashboard",
            _ => "/Koc/Dashboard"
        });
    }
}
