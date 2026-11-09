using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class ActivateAccountModel(IAccountActivationService accountActivationService, IEmailSender emailSender, IEmailTemplateRenderer emailTemplateRenderer, IConfiguration configuration) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public bool IsSuccess { get; private set; }
    public bool WasSubmitted { get; private set; }
    public bool WasResendRequested { get; private set; }
    public bool HasDeliveryWarning => !string.IsNullOrWhiteSpace(EmailWarning);
    public string? Message { get; private set; }
    [BindProperty] public string Email { get; set; } = string.Empty;
    [TempData] public string? EmailWarning { get; set; }

    public async Task OnGetAsync([FromQuery] string? token, [FromQuery] string? email, CancellationToken cancellationToken)
    {
        Email = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            Message = EmailWarning ?? (string.IsNullOrWhiteSpace(Email) ? "Nhập email và OTP đã nhận để kích hoạt tài khoản." : "OTP đã được gửi tới email đăng ký.");
            return;
        }
        var result = await accountActivationService.ActivateByTokenAsync(token ?? string.Empty, cancellationToken);
        IsSuccess = result.Success;
        Message = result.Message;
    }

    public async Task<IActionResult> OnPostAsync(string code, CancellationToken cancellationToken)
    {
        WasSubmitted = true;
        var result = await accountActivationService.ActivateByCodeAsync(Email, code, cancellationToken);
        IsSuccess = result.Success;
        Message = result.Message;
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(CancellationToken cancellationToken)
    {
        WasResendRequested = true;
        Email = Email?.Trim() ?? string.Empty;
        var result = await accountActivationService.CreateActivationTokenByEmailAsync(Email, TimeSpan.FromHours(24), cancellationToken);
        if (result.Success && result.Data is not null)
        {
            var baseUrl = configuration["Email:ActivationBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}/kich-hoat-tai-khoan";
            var verifyUrl = $"{baseUrl}?token={result.Data.RawToken}";
            var content = $@"<p>Mã OTP kích hoạt mới của bạn:</p>
                <p style=""font-size:28px;font-weight:700;letter-spacing:6px;text-align:center;"">{result.Data.RawCode}</p>
                <p>Hoặc dùng <a href=""{System.Net.WebUtility.HtmlEncode(verifyUrl)}"">liên kết kích hoạt này</a>.</p>
                <p>Mã có hiệu lực 24 giờ. Không chia sẻ mã với bất kỳ ai.</p>";
            var html = emailTemplateRenderer.Render(new EmailTemplateModel
            {
                ProjectName = "TopStar Platform", MainTitle = "Mã kích hoạt mới",
                Preheader = "Mã OTP kích hoạt tài khoản TopStar.", BodyHtml = content
            });
            await emailSender.SendAsync(Email, "Mã kích hoạt mới TopStar", html, cancellationToken);
        }
        Message = "Nếu email hợp lệ và chưa kích hoạt, hệ thống sẽ gửi mã mới. Vui lòng kiểm tra cả thư mục spam.";
        return Page();
    }
}
