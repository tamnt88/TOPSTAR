using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Topstar.Application;
using Topstar.Application.SystemSettings;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
[EnableRateLimiting("registration")]
public sealed class RegisterModel(
    IAuthService authService,
    ISystemSettingsAdminService systemSettings,
    IAccountActivationService accountActivationService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer,
    IConfiguration configuration) : AuthPageBase
{
    [BindProperty]
    public QuickKocInput KocInput { get; set; } = new();

    [BindProperty]
    public QuickBrandInput BrandInput { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string ActiveTab { get; private set; } = "koc";
    public bool GoogleRegistrationEnabled { get; private set; }
    public bool FacebookRegistrationEnabled { get; private set; }
    [BindProperty] public string? Website { get; set; }
    [BindProperty] public long FormStartedAt { get; set; }

    public sealed class QuickKocInput
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class QuickBrandInput
    {
        [Display(Name = "Tên người đại diện")]
        [Required(ErrorMessage = "Vui lòng nhập tên người đại diện.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Tên công ty/Brand")]
        [Required(ErrorMessage = "Vui lòng nhập tên công ty/Brand.")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? tab = null, CancellationToken cancellationToken = default)
    {
        ActiveTab = NormalizeTab(tab);
        FormStartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await LoadRegistrationOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostQuickKocAsync(CancellationToken cancellationToken)
    {
        ActiveTab = "koc";
        if (IsAutomatedSubmission()) return await RejectAutomatedAsync(cancellationToken);
        ModelState.Clear();
        if (!TryValidateModel(KocInput, nameof(KocInput)))
        {
            await LoadRegistrationOptionsAsync(cancellationToken);
            return Page();
        }

        var result = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Koc,
                KocInput.Email,
                null,
                null,
                KocInput.Password,
                KocInput.FullName,
                null,
                null,
                BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            await LoadRegistrationOptionsAsync(cancellationToken);
            return Page();
        }

        await SendActivationEmailAsync(result.Data, KocInput.FullName, cancellationToken);
        return RedirectToPage("/Public/ActivateAccount", new { email = KocInput.Email });
    }

    public async Task<IActionResult> OnPostQuickBrandAsync(CancellationToken cancellationToken)
    {
        ActiveTab = "brand";
        if (IsAutomatedSubmission()) return await RejectAutomatedAsync(cancellationToken);
        ModelState.Clear();
        if (!TryValidateModel(BrandInput, nameof(BrandInput)))
        {
            await LoadRegistrationOptionsAsync(cancellationToken);
            return Page();
        }

        var result = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Brand,
                BrandInput.Email,
                null,
                null,
                BrandInput.Password,
                BrandInput.FullName,
                BrandInput.CompanyName,
                null,
                BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            await LoadRegistrationOptionsAsync(cancellationToken);
            return Page();
        }

        await SendActivationEmailAsync(result.Data, BrandInput.FullName, cancellationToken);
        return RedirectToPage("/Public/ActivateAccount", new { email = BrandInput.Email });
    }

    private static string NormalizeTab(string? tab)
        => string.Equals(tab, "brand", StringComparison.OrdinalIgnoreCase) ? "brand" : "koc";

    private bool IsAutomatedSubmission()
    {
        if (!string.IsNullOrWhiteSpace(Website)) return true;
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - FormStartedAt;
        return FormStartedAt <= 0 || elapsed < 2 || elapsed > 7200;
    }

    private async Task<IActionResult> RejectAutomatedAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = "Không thể xử lý đăng ký tự động. Vui lòng tải lại trang và thử lại.";
        FormStartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await LoadRegistrationOptionsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadRegistrationOptionsAsync(CancellationToken cancellationToken)
    {
        var options = await systemSettings.GetSocialLoginConfigurationsAsync(cancellationToken);
        var isBrand = ActiveTab == "brand";
        var google = options.First(x => x.Provider == "Google");
        var facebook = options.First(x => x.Provider == "Facebook");
        GoogleRegistrationEnabled = google.RegistrationEnabled && (isBrand ? google.BrandEnabled : google.KocEnabled);
        FacebookRegistrationEnabled = facebook.RegistrationEnabled && (isBrand ? facebook.BrandEnabled : facebook.KocEnabled);
    }

    private async Task SendActivationEmailAsync(
        AuthenticatedUser user,
        string fullName,
        CancellationToken cancellationToken)
    {
        var tokenResult = await accountActivationService.CreateActivationTokenAsync(
            user.UserId,
            TimeSpan.FromHours(24),
            cancellationToken);

        if (!tokenResult.Success || tokenResult.Data is null)
        {
            TempData["EmailWarning"] = "Tài khoản đã được tạo nhưng chưa tạo được mã kích hoạt. Vui lòng dùng chức năng gửi lại mã.";
            return;
        }

        var baseUrl = configuration["Email:ActivationBaseUrl"]?.TrimEnd('/')
            ?? $"{Request.Scheme}://{Request.Host}/kich-hoat-tai-khoan";
        var verifyUrl = $"{baseUrl}?token={Uri.EscapeDataString(tokenResult.Data.RawToken)}";
        var encodedName = System.Net.WebUtility.HtmlEncode(fullName);
        var encodedUrl = System.Net.WebUtility.HtmlEncode(verifyUrl);
        var contentHtml = $@"
            <p>Xin chào {encodedName},</p>
            <p>Nhập mã OTP dưới đây tại trang kích hoạt tài khoản:</p>
            <p style=""font-size:28px;font-weight:700;letter-spacing:6px;text-align:center;"">{tokenResult.Data.RawCode}</p>
            <p>Hoặc bấm vào nút bên dưới để kích hoạt bằng liên kết:</p>
            <p style=""margin:20px 0;""><a href=""{encodedUrl}"" style=""display:inline-block;padding:12px 18px;background:#6f2bd9;color:#fff;text-decoration:none;border-radius:10px;font-weight:700;"">Kích hoạt tài khoản</a></p>
            <p>OTP và liên kết có hiệu lực trong 24 giờ. Không cung cấp mã này cho bất kỳ ai.</p>";
        var htmlBody = emailTemplateRenderer.Render(new EmailTemplateModel
        {
            ProjectName = "TopStar Platform",
            MainTitle = "Kích hoạt tài khoản",
            Preheader = "Hoàn tất bước kích hoạt tài khoản TopStar.",
            BodyHtml = contentHtml
        });

        var sent = await emailSender.SendAsync(
            user.Email,
            "Kích hoạt tài khoản TopStar",
            htmlBody,
            cancellationToken);

        if (!sent)
        {
            TempData["EmailWarning"] = "Tài khoản đã được tạo nhưng chưa gửi được email kích hoạt. Vui lòng dùng chức năng gửi lại mã.";
        }
    }
}
