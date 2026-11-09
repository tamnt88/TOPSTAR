using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;
using Topstar.Application.Dashboard;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class LoginModel(
    IAuthService authService,
    IAuthRepository authRepository,
    IAuthenticationSchemeProvider schemeProvider,
    IDashboardDataService dashboardDataService,
    ISystemSettingsAdminService systemSettings) : AuthPageBase
{
    [TempData]
    public string? FlashMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ExternalError { get; set; }

    public string? ErrorMessage { get; private set; }
    public bool NeedsActivation { get; private set; }
    public bool GoogleLoginEnabled { get; private set; }
    public bool FacebookLoginEnabled { get; private set; }

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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSocialOptionsAsync(cancellationToken);
        if (string.Equals(ExternalError, "oauth-session", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Phiên đăng nhập Google/Facebook đã hết hạn hoặc được mở từ tên miền khác. Vui lòng bấm đăng nhập lại.";
        }
        else if (!string.IsNullOrWhiteSpace(FlashMessage))
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
            NeedsActivation = result.Message.Contains("chưa được xác minh", StringComparison.OrdinalIgnoreCase);
            return Page();
        }

        await SignInUserAsync(result.Data, Input.RememberMe);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return Redirect(await ResolvePostLoginRedirectAsync(result.Data, cancellationToken));
    }

    public async Task<IActionResult> OnPostExternalAsync(string provider, string mode = "login", string role = "Koc", string? returnUrl = null)
    {
        if (!IsProviderSupported(provider))
        {
            ErrorMessage = "Nhà cung cấp đăng nhập không hợp lệ.";
            return Page();
        }

        var option = await systemSettings.GetSocialLoginConfigurationAsync(provider, HttpContext.RequestAborted);
        var isRegistration = string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase);
        var roleEnabled = string.Equals(role, "Brand", StringComparison.OrdinalIgnoreCase) ? option.BrandEnabled : option.KocEnabled;
        if (!(isRegistration ? option.RegistrationEnabled : option.LoginEnabled) || !roleEnabled)
        {
            ErrorMessage = $"Đăng nhập bằng {provider} đang tạm tắt.";
            await LoadSocialOptionsAsync(HttpContext.RequestAborted);
            return Page();
        }

        if (await schemeProvider.GetSchemeAsync(provider) is null)
        {
            ErrorMessage = $"Đăng nhập bằng {provider} chưa được cấu hình. Vui lòng liên hệ quản trị viên.";
            return Page();
        }

        var redirectUrl = Url.Page(
            "/Public/Login",
            "ExternalCallback",
            new { returnUrl = returnUrl ?? "/", provider, mode, role });

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            provider);
    }

    public async Task<IActionResult> OnGetExternalCallbackAsync(string? returnUrl = null, string provider = "Google", string mode = "login", string role = "Koc", CancellationToken cancellationToken = default)
    {
        var externalResult = await HttpContext.AuthenticateAsync("External");
        if (!externalResult.Succeeded || externalResult.Principal is null)
        {
            FlashMessage = "Không thể xác thực từ mạng xã hội. Vui lòng thử lại.";
            return RedirectToPage("/Public/Login");
        }

        var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email)
            ?? externalResult.Principal.FindFirstValue("email");
        var name = externalResult.Principal.FindFirstValue(ClaimTypes.Name)
            ?? externalResult.Principal.Identity?.Name
            ?? "Người dùng";

        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("External");
            FlashMessage = "Không lấy được email từ nhà cung cấp. Vui lòng dùng cách đăng ký thủ công.";
            return RedirectToPage("/Public/Login");
        }

        AuthenticatedUser user;
        var existing = await authRepository.FindUserByEmailOrPhoneAsync(email, cancellationToken);

        if (existing is not null)
        {
            if (!existing.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = "Tài khoản đang bị khóa hoặc vô hiệu hóa.";
                return RedirectToPage("/Public/Login");
            }

            if (existing.LockoutEndAtUtc.HasValue && existing.LockoutEndAtUtc.Value > DateTime.UtcNow)
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.";
                return RedirectToPage("/Public/Login");
            }

            if (!existing.EmailConfirmed)
            {
                await authRepository.ConfirmEmailAsync(existing.UserId, cancellationToken);
            }

            user = MapToAuthenticated(existing);
        }
        else if (!CanCreateExternalAccount(provider, mode))
        {
            await HttpContext.SignOutAsync("External");
            FlashMessage = "Email này chưa có tài khoản. Vui lòng đăng ký nhanh bằng Google/Facebook ở trang đăng ký.";
            return RedirectToPage("/Public/Login");
        }
        else
        {
            var userRole = ParseRole(role);
            var option = await systemSettings.GetSocialLoginConfigurationAsync(provider, cancellationToken);
            var roleEnabled = userRole == UserRoleType.Brand ? option.BrandEnabled : option.KocEnabled;
            if (!option.RegistrationEnabled || !roleEnabled)
            {
                await HttpContext.SignOutAsync("External");
                FlashMessage = $"Email này chưa có tài khoản và đăng ký bằng {provider} đang tạm tắt.";
                return RedirectToPage("/Public/Login");
            }

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
            await authRepository.ConfirmEmailAsync(user.UserId, cancellationToken);
        }

        await HttpContext.SignOutAsync("External");
        await SignInUserAsync(user, true);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect(await ResolvePostLoginRedirectAsync(user, cancellationToken));
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

    private static bool CanCreateExternalAccount(string provider, string mode)
        => string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase)
           || (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase)
               && string.Equals(mode, "login-or-register", StringComparison.OrdinalIgnoreCase));

    private async Task LoadSocialOptionsAsync(CancellationToken cancellationToken)
    {
        var options = await systemSettings.GetSocialLoginConfigurationsAsync(cancellationToken);
        GoogleLoginEnabled = options.First(x => x.Provider == "Google").LoginEnabled;
        FacebookLoginEnabled = options.First(x => x.Provider == "Facebook").LoginEnabled;
    }

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

    private async Task<string> ResolvePostLoginRedirectAsync(AuthenticatedUser user, CancellationToken cancellationToken)
    {
        if (user.Role == UserRoleType.Brand)
        {
            var brandData = await dashboardDataService.GetBrandDashboardAsync(user.UserId, cancellationToken);
            return brandData.NeedsProfileCompletion
                ? "/Brand/EditProfile?forceComplete=1"
                : "/Brand/Dashboard";
        }

        if (user.Role != UserRoleType.Koc)
        {
            return user.Role switch
            {
                UserRoleType.Admin => "/Admin/Dashboard",
                _ => "/Koc/Dashboard"
            };
        }

        var kocData = await dashboardDataService.GetKocDashboardAsync(user.UserId, cancellationToken);
        return kocData.NeedsProfileCompletion
            ? "/Koc/Profile?forceComplete=1"
            : "/Koc/Dashboard";
    }
}
