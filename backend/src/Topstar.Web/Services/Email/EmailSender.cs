using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Email;

public interface IEmailSender
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class EmailSender(IConfiguration configuration, TopstarDbContext db, IEmailLogoEmbedder logoEmbedder, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = await ResolveSettingsAsync(cancellationToken);
        var host = settings.Host;
        var port = settings.Port;
        var username = settings.Username;
        var password = settings.Password;
        var enableSsl = settings.EnableSsl;
        var fromEmail = settings.FromEmail;
        var fromName = settings.FromName;

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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = await logoEmbedder.BuildBodyAsync(htmlBody, cancellationToken);
            var normalizedPassword = password!.Replace(" ", string.Empty).Trim();
            var socketOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(username, normalizedPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (SmtpCommandException ex)
        {
            logger.LogError(ex,
                "SMTP từ chối gửi email tới {ToEmail}. SMTP={Host}:{Port}, User={User}, SSL={Ssl}.",
                toEmail, host, port, username, enableSsl);
            return false;
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogError(ex,
                "Lỗi giao thức SMTP khi gửi tới {ToEmail}. SMTP={Host}:{Port}, SSL={Ssl}.",
                toEmail, host, port, enableSsl);
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

    private async Task<SmtpSettings> ResolveSettingsAsync(CancellationToken cancellationToken)
    {
        var account = await db.SystemEmailAccounts.AsNoTracking()
            .Where(x => x.IsActive && x.IsDefaultSender)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new { x.SmtpHost, x.SmtpPort, x.SmtpUsername, x.SmtpPassword, x.SmtpEnableSsl, x.EmailAddress, x.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);
        if (account is not null)
            return new(account.SmtpHost, account.SmtpPort ?? 587, account.SmtpUsername, account.SmtpPassword, account.SmtpEnableSsl, account.EmailAddress, account.DisplayName);

        var port = int.TryParse(configuration["Email:Smtp:Port"], out var parsed) ? parsed : 587;
        var ssl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enabled) && enabled;
        var username = configuration["Email:Smtp:Username"];
        return new(configuration["Email:Smtp:Host"], port, username, configuration["Email:Smtp:Password"], ssl,
            configuration["Email:FromAddress"] ?? username, configuration["Email:FromName"] ?? "TopStar");
    }

    private sealed record SmtpSettings(string? Host,int Port,string? Username,string? Password,bool EnableSsl,string? FromEmail,string FromName);
}
