using System.Net;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Email;

public interface IEmailLogoEmbedder
{
    Task<MimeEntity> BuildBodyAsync(string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class EmailLogoEmbedder(
    TopstarDbContext db,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<EmailLogoEmbedder> logger) : IEmailLogoEmbedder
{
    private const string Placeholder = "{{TOPSTAR_EMAIL_LOGO}}";
    private const string ContentId = "topstar-email-logo";

    public async Task<MimeEntity> BuildBodyAsync(string htmlBody, CancellationToken cancellationToken = default)
    {
        var configuredLogo = await db.SystemWebsiteConfigs.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => x.LogoUrl)
            .FirstOrDefaultAsync(cancellationToken)
            ?? configuration["Email:Template:LogoUrl"]
            ?? "/images/logo.png";

        var builder = new BodyBuilder();
        var localPath = ResolveLocalPath(configuredLogo);
        if (localPath is not null && File.Exists(localPath))
        {
            var resource = builder.LinkedResources.Add(localPath);
            resource.ContentId = ContentId;
            resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            htmlBody = htmlBody.Replace(Placeholder, $"cid:{ContentId}", StringComparison.Ordinal);
        }
        else if (Uri.TryCreate(configuredLogo, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && !uri.IsLoopback)
        {
            htmlBody = htmlBody.Replace(Placeholder, WebUtility.HtmlEncode(uri.AbsoluteUri), StringComparison.Ordinal);
        }
        else
        {
            logger.LogWarning("Không tìm thấy logo email tại {Logo}. Dùng logo mặc định.", configuredLogo);
            var fallback = Path.Combine(environment.WebRootPath, "images", "logo.png");
            if (File.Exists(fallback))
            {
                var resource = builder.LinkedResources.Add(fallback);
                resource.ContentId = ContentId;
                resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                htmlBody = htmlBody.Replace(Placeholder, $"cid:{ContentId}", StringComparison.Ordinal);
            }
            else
            {
                htmlBody = htmlBody.Replace(Placeholder, string.Empty, StringComparison.Ordinal);
            }
        }

        builder.HtmlBody = htmlBody;
        return builder.ToMessageBody();
    }

    private string? ResolveLocalPath(string logoUrl)
    {
        var relativePath = logoUrl.Trim();
        if (Uri.TryCreate(relativePath, UriKind.Absolute, out var uri))
        {
            if (!uri.IsLoopback) return null;
            relativePath = uri.AbsolutePath;
        }

        if (!relativePath.StartsWith('/')) return null;
        var webRoot = Path.GetFullPath(environment.WebRootPath);
        var candidate = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        return candidate.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }
}
