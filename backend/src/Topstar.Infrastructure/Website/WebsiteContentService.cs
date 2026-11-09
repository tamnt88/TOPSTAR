using Microsoft.EntityFrameworkCore;
using Topstar.Application.Website;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Website;

public sealed class WebsiteContentService(TopstarDbContext db) : IWebsiteContentService
{
    public async Task<WebsiteContentItem?> GetWebsiteContentAsync(CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemWebsiteConfigs.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new WebsiteContentItem(
                x.WebsiteName,
                x.ShortName,
                x.Domain,
                x.Address,
                x.ContactEmail,
                x.Hotline,
                x.LogoUrl,
                x.FaviconUrl,
                x.DefaultShareImageUrl,
                x.CopyrightText))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return new WebsiteContentItem(
                "TopStar",
                null,
                null,
                null,
                null,
                null,
                "/images/logo.png",
                "/images/logo.png",
                "/images/logo.png",
                null);
        }

        return entity;
    }
}
