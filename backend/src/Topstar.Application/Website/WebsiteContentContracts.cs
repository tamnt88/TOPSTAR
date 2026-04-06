namespace Topstar.Application.Website;

public interface IWebsiteContentService
{
    Task<WebsiteContentItem?> GetWebsiteContentAsync(CancellationToken cancellationToken = default);
}

public sealed record WebsiteContentItem(
    string WebsiteName,
    string? ShortName,
    string? Domain,
    string? Address,
    string? ContactEmail,
    string? Hotline,
    string? LogoUrl,
    string? FaviconUrl,
    string? DefaultShareImageUrl,
    string? CopyrightText);

