using System.Net;
using System.Net.Mail;

namespace Topstar.Web.Services.Email;

public interface IEmailSender
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:Smtp:Host"];
        var port = int.TryParse(configuration["Email:Smtp:Port"], out var p) ? p : 587;
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var enableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var ssl) && ssl;
        var fromEmail = configuration["Email:FromAddress"] ?? username;
        var fromName = configuration["Email:FromName"] ?? "TopStar";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            logger.LogWarning("Email SMTP chưa cấu hình. Bỏ qua gửi email tới {Email}.", toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Thiếu SMTP username/password. Không thể gửi email tới {Email}.", toEmail);
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail));
            var normalizedPassword = password?.Replace(" ", string.Empty).Trim();
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, normalizedPassword)
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex,
                "Gửi email thất bại tới {ToEmail}. SMTP={Host}:{Port}, User={User}, SSL={Ssl}.",
                toEmail, host, port, username, enableSsl);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Gửi email lỗi không xác định tới {ToEmail}. SMTP={Host}:{Port}, User={User}.",
                toEmail, host, port, username);
            return false;
        }
    }
}