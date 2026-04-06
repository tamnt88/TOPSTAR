using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class LoginModel(
    IAuthService authService,
    IAuthRepository authRepository,
    IAuthenticationSchemeProvider schemeProvider) : AuthPageBase
{
    [TempData]
    public string? FlashMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Display(Name = "Email hoac so dien thoai")]
        [Required(ErrorMessage = "Vui long nhap email hoac so dien thoai")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Display(Name = "Mat khau")]
        [Required(ErrorMessage = "Vui long nhap mat khau")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet()
    {
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            ErrorMessage = FlashMessage;
        }
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

        await SignInUserAsync(result.Data, Input.RememberMe);
        return Redirect(result.Data.Role switch
        {
            UserRoleType.Admin => "/Admin/Dashboard",
            UserRoleType.Brand => "/Brand/Dashboard",
            _ => "/Koc/Dashboard"
        });
    }

    public async Task<IActionResult> OnPostExternalAsync(string provider, string mode = "login", string role = "Koc", string? returnUrl = null)
    {
        if (!IsProviderSupported(provider))
        {
            ErrorMessage = "Nha cung cap dang nhap khong hop le.";
            return Page();
        }

        if (await schemeProvider.GetSchemeAsync(provider) is null)
        {
            ErrorMessage = $"Dang nhap bang {provider} chua duoc cau hinh. Vui long lien he quan tri vien.";
            return Page();
        }

        var redirectUrl = Url.Page(
            "/Public/Login",
            "ExternalCallback",
            new { returnUrl = returnUrl ?? "/", mode, role });

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            provider);
    }

    public async Task<IActionResult> OnGetExternalCallbackAsync(string? returnUrl = null, string mode = "login", string role = "Koc", CancellationToken cancellationToken = default)
    {
        var externalResult = await HttpContext.AuthenticateAsync("External");
        if (!externalResult.Succeeded || externalResult.Principal is null)
        {
            FlashMessage = "Khong the xac thuc tu mang xa hoi. Vui long thu lai.";
            return RedirectToPage("/Public/Login");
        }

        var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email)
            ?? externalResult.Principal.FindFirstValue("email");
        var name = externalResult.Principal.FindFirstValue(ClaimTypes.Name)
            ?? externalResult.Principal.Identity?.Name
            ?? "Nguoi dung";

        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("External");
            FlashMessage = "Khong lay duoc email tu nha cung cap. Vui long dung cach dang ky thu cong.";
            return RedirectToPage("/Public/Login");
        }

        AuthenticatedUser user;
        var existing = await authRepository.FindUserByEmailOrPhoneAsync(email, cancellationToken);

        if (existing is not null)
        {
            if (!existing.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = "Tai khoan dang bi khoa hoac vo hieu hoa.";
                return RedirectToPage("/Public/Login");
            }

            if (existing.LockoutEndAtUtc.HasValue && existing.LockoutEndAtUtc.Value > DateTime.UtcNow)
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = "Tai khoan tam thoi bi khoa. Vui long thu lai sau.";
                return RedirectToPage("/Public/Login");
            }

            user = MapToAuthenticated(existing);
        }
        else if (!string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync("External");
            FlashMessage = "Email nay chua co tai khoan. Vui long dang ky nhanh bang Google/Facebook o trang dang ky.";
            return RedirectToPage("/Public/Login");
        }
        else
        {
            var userRole = ParseRole(role);
            var generatedPassword = GenerateStrongPassword();
            var register = await authService.RegisterAsync(
                new RegisterRequest(
                    userRole,
                    email,
                    null,
                    null,
                    generatedPassword,
                    name,
                    userRole == UserRoleType.Brand ? name : null,
                    null,
                    BuildDeviceContext()),
                cancellationToken);

            if (!register.Success || register.Data is null)
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = register.Message;
                return RedirectToPage("/Public/Login");
            }

            user = register.Data;
        }

        await HttpContext.SignOutAsync("External");
        await SignInUserAsync(user, true);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect(user.Role switch
        {
            UserRoleType.Admin => "/Admin/Dashboard",
            UserRoleType.Brand => "/Brand/Dashboard",
            _ => "/Koc/Dashboard"
        });
    }

    private async Task SignInUserAsync(AuthenticatedUser user, bool isPersistent)
    {
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
            IsPersistent = isPersistent,
            ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(1)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
    }

    private static bool IsProviderSupported(string provider)
        => string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "Facebook", StringComparison.OrdinalIgnoreCase);

    private static UserRoleType ParseRole(string? role)
        => role?.Trim().ToUpperInvariant() switch
        {
            "BRAND" => UserRoleType.Brand,
            "ADMIN" => UserRoleType.Admin,
            _ => UserRoleType.Koc
        };

    private static string GenerateStrongPassword()
        => $"Ext@{Guid.NewGuid():N}";

    private static AuthenticatedUser MapToAuthenticated(AuthUserRow row)
    {
        var role = row.RoleCode.ToUpperInvariant() switch
        {
            "ADMIN" => UserRoleType.Admin,
            "BRAND" => UserRoleType.Brand,
            _ => UserRoleType.Koc
        };

        return new AuthenticatedUser(row.UserId, row.Email, row.Phone, role, row.DisplayName);
    }
}
