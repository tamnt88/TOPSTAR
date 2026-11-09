using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Email;

public interface IEmailAccountDiagnostics
{
    Task<(bool Success, string Message)> TestServerAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendTestAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public sealed class EmailAccountDiagnostics(TopstarDbContext db, IEmailTemplateRenderer templates, IEmailLogoEmbedder logoEmbedder, ILogger<EmailAccountDiagnostics> logger) : IEmailAccountDiagnostics
{
    public async Task<(bool Success, string Message)> TestServerAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.SystemEmailAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (account is null) return (false, "Không tìm thấy tài khoản email.");
        if (string.IsNullOrWhiteSpace(account.SmtpHost) || !account.SmtpPort.HasValue)
            return (false, "Tài khoản chưa có đủ SMTP Host và Port.");

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var socketOptions = account.SmtpPort.Value == 465
                ? SecureSocketOptions.SslOnConnect
                : account.SmtpEnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            using var client = new SmtpClient();
            await client.ConnectAsync(account.SmtpHost, account.SmtpPort.Value, socketOptions, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
            return (true, $"Đã kết nối TLS tới {account.SmtpHost}:{account.SmtpPort.Value} thành công.");
        }
        catch (Exception ex) when (ex is SmtpProtocolException or System.Net.Sockets.SocketException or OperationCanceledException or System.Security.Authentication.AuthenticationException)
        {
            logger.LogWarning(ex, "SMTP server check failed for account {AccountId}", accountId);
            return (false, "Không thể kết nối máy chủ SMTP. Vui lòng kiểm tra Host, Port và firewall.");
        }
    }

    public async Task<(bool Success, string Message)> SendTestAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.SystemEmailAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (account is null) return (false, "Không tìm thấy tài khoản email.");
        if (!account.IsActive) return (false, "Tài khoản đang tạm tắt.");
        if (string.IsNullOrWhiteSpace(account.SmtpHost) || !account.SmtpPort.HasValue || string.IsNullOrWhiteSpace(account.SmtpUsername) || string.IsNullOrWhiteSpace(account.SmtpPassword))
            return (false, "Tài khoản chưa có đầy đủ cấu hình SMTP.");

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(account.DisplayName, account.EmailAddress));
            message.To.Add(MailboxAddress.Parse(account.EmailAddress));
            message.Subject = "[TopStar] Kiểm tra cấu hình email";
            var testHtml = templates.Render(new EmailTemplateModel
            {
                ProjectName = "TopStar Platform",
                MainTitle = "Kiểm tra cấu hình email",
                Preheader = "Email kiểm tra cấu hình SMTP TopStar.",
                BodyHtml = $"<p>Email kiểm tra được gửi thành công lúc <strong>{DateTime.Now:HH:mm dd/MM/yyyy}</strong>.</p><p>Logo trong email này được lấy từ cấu hình Nhận diện của Admin.</p>"
            });
            message.Body = await logoEmbedder.BuildBodyAsync(testHtml, cancellationToken);
            var socketOptions = account.SmtpPort.Value == 465
                ? SecureSocketOptions.SslOnConnect
                : account.SmtpEnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            using var client = new SmtpClient();
            await client.ConnectAsync(account.SmtpHost, account.SmtpPort.Value, socketOptions, cancellationToken);
            await client.AuthenticateAsync(account.SmtpUsername, account.SmtpPassword.Replace(" ", string.Empty).Trim(), cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return (true, $"Đã gửi email thử đến {account.EmailAddress}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP test mail failed for account {AccountId}", accountId);
            return (false, "Gửi email thử thất bại. Vui lòng kiểm tra tài khoản, mật khẩu ứng dụng và SSL.");
        }
    }
}
