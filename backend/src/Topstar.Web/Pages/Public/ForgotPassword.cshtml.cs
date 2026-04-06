using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class ForgotPasswordModel(
    IAuthService authService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer,
    IConfiguration configuration) : AuthPageBase
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại")]
        public string EmailOrPhone { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var lookup = Input.EmailOrPhone.Trim();
        var result = await authService.RequestPasswordResetAsync(
            new ForgotPasswordRequest(lookup, BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        var baseUrl = configuration["Email:ResetPasswordBaseUrl"]?.TrimEnd('/')
            ?? $"{Request.Scheme}://{Request.Host}/dat-lai-mat-khau";
        var resetUrl = $"{baseUrl}?token={result.Data.ResetToken}";

        var bodyHtml = $@"
            <p>Xin chào,</p>
            <p>Hệ thống vừa nhận yêu cầu quên mật khẩu cho tài khoản của bạn.</p>
            <p style=""margin:20px 0;"">
                <a href=""{resetUrl}"" style=""display:inline-block;padding:12px 18px;background:linear-gradient(90deg,#e87b2e 0%,#f1c24b 100%);color:#fff;text-decoration:none;border-radius:10px;font-weight:700;"">
                    Đặt lại mật khẩu
                </a>
            </p>
            <p>Nếu nút không hoạt động, bạn có thể dùng liên kết sau:</p>
            <p><a href=""{resetUrl}"">{resetUrl}</a></p>
            <p>Liên kết có hiệu lực đến: <strong>{result.Data.ExpiresAtUtc.AddHours(7):dd/MM/yyyy HH:mm}</strong> (GMT+7).</p>
            <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";

        var emailHtml = emailTemplateRenderer.Render(new EmailTemplateModel
        {
            ProjectName = "TopStar Platform",
            MainTitle = "Đặt lại mật khẩu",
            Preheader = "Liên kết đặt lại mật khẩu tài khoản TopStar.",
            BodyHtml = bodyHtml
        });

        // Nếu user nhập số điện thoại thì hiện tại hệ thống chưa có luồng SMS, chỉ hỗ trợ email.
        var sendTo = lookup.Contains('@') ? lookup : result.Data.UserEmail;
        var sent = await emailSender.SendAsync(sendTo, "Yêu cầu đặt lại mật khẩu TopStar", emailHtml, cancellationToken);
        if (!sent)
        {
            ErrorMessage = "Không thể gửi email lúc này. Vui lòng kiểm tra cấu hình SMTP hoặc thử lại sau.";
            return Page();
        }

        SuccessMessage = "Email đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư (và Spam).";
        return Page();
    }
}

