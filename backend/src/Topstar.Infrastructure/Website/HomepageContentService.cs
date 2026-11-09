using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Website;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Website;

public sealed class HomepageContentService(TopstarDbContext db) : IHomepageContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<HomepageContent> GetAsync(CancellationToken cancellationToken = default)
    {
        string? json;
        try
        {
            json = await db.SystemWebsiteConfigs.AsNoTracking()
                .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
                .Select(x => x.HomepageContentJson)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 207)
        {
            return HomepageContent.Default;
        }

        if (string.IsNullOrWhiteSpace(json)) return HomepageContent.Default;
        try
        {
            return JsonSerializer.Deserialize<HomepageContent>(json, JsonOptions) ?? HomepageContent.Default;
        }
        catch (JsonException)
        {
            return HomepageContent.Default;
        }
    }

    public async Task<HomepageSaveResult> SaveAsync(HomepageContent content, string reason, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            return new HomepageSaveResult(false, "Vui lòng nhập lý do thay đổi từ 10 đến 1.000 ký tự.");
        if (string.IsNullOrWhiteSpace(content.HeroTitle) || string.IsNullOrWhiteSpace(content.HeroDescription))
            return new HomepageSaveResult(false, "Tiêu đề và mô tả banner là bắt buộc.");

        var entity = await db.SystemWebsiteConfigs.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new SystemWebsiteConfigEntity
            {
                Id = Guid.NewGuid(), WebsiteName = "TopStar", IsActive = true, IsVisible = true, CreatedAtUtc = DateTime.UtcNow
            };
            db.SystemWebsiteConfigs.Add(entity);
        }

        var json = JsonSerializer.Serialize(Normalize(content), JsonOptions);
        entity.HomepageContentJson = json;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.HomepageContentHistories.Add(new HomepageContentHistoryEntity
        {
            Id = Guid.NewGuid(), ContentJson = json, Reason = reason.Trim(), ActorUserId = actorUserId, CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return new HomepageSaveResult(true, "Đã cập nhật nội dung trang chủ và lưu lịch sử thay đổi.");
    }

    public async Task<IReadOnlyList<HomepageVersionItem>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.HomepageContentHistories.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(20)
                .Select(x => new HomepageVersionItem(x.Id, x.Reason, x.ActorUserId, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 208)
        {
            return [];
        }
    }

    private static HomepageContent Normalize(HomepageContent x) => x with
    {
        TrustLabel = x.TrustLabel.Trim(), HeroTitle = x.HeroTitle.Trim(), HeroDescription = x.HeroDescription.Trim(),
        PrimaryCtaText = x.PrimaryCtaText.Trim(), PrimaryCtaUrl = x.PrimaryCtaUrl.Trim(),
        SecondaryCtaText = x.SecondaryCtaText.Trim(), SecondaryCtaUrl = x.SecondaryCtaUrl.Trim(), HeroImageUrl = x.HeroImageUrl.Trim(),
        Creators = x.Creators.OrderBy(i => i.DisplayOrder).Take(12).ToArray(),
        Features = x.Features.OrderBy(i => i.DisplayOrder).Take(8).ToArray(),
        ProcessSteps = x.ProcessSteps.OrderBy(i => i.DisplayOrder).Take(8).ToArray(),
        Metrics = x.Metrics.OrderBy(i => i.DisplayOrder).Take(8).ToArray(),
        Partners = x.Partners.OrderBy(i => i.DisplayOrder).Take(24).ToArray()
    };
}
