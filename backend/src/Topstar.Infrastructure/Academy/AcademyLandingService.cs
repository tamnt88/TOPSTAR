using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Academy;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Academy;

public sealed class AcademyLandingService(TopstarDbContext db) : IAcademyLandingService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<AcademyLandingContent> GetAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await db.SystemWebsiteConfigs.AsNoTracking().Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
                .Select(x => x.AcademyContentJson).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(json)) return AcademyLandingContent.Default;

            var content = JsonSerializer.Deserialize<AcademyLandingContent>(json, Options) ?? AcademyLandingContent.Default;
            // Bản thử nghiệm cũ dùng ảnh icon nhỏ và câu chữ mẫu; thay bằng bộ mặc định đã biên tập.
            return content.HeroImageUrl.Contains("/images/icon/2.png", StringComparison.OrdinalIgnoreCase)
                ? AcademyLandingContent.Default
                : content;
        }
        catch
        {
            return AcademyLandingContent.Default;
        }
    }

    public async Task<AcademyLandingSaveResult> SaveAsync(AcademyLandingContent content, string reason, Guid? actor, CancellationToken ct = default)
    {
        reason = reason?.Trim() ?? "";
        if (reason.Length is < 10 or > 1000) return Fail("Vui lòng nhập lý do thay đổi từ 10 đến 1.000 ký tự.");
        if (string.IsNullOrWhiteSpace(content.HeroTitle) || string.IsNullOrWhiteSpace(content.HeroDescription) || string.IsNullOrWhiteSpace(content.FinalCtaTitle))
            return Fail("Vui lòng nhập đầy đủ nội dung bắt buộc.");
        if (content.Roadmap.Count is < 1 or > 8 || content.TrustItems.Count is < 1 or > 8 || content.QualityItems.Count is < 1 or > 6 || content.Outcomes.Count is < 1 or > 8 || content.Resources.Count is < 1 or > 8)
            return Fail("Số lượng nội dung động không hợp lệ.");

        var entity = await db.SystemWebsiteConfigs.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (entity is null) return Fail("Chưa có cấu hình website để lưu nội dung Học viện.");

        var normalized = Normalize(content);
        var json = JsonSerializer.Serialize(normalized, Options);
        entity.AcademyContentJson = json;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.AcademyContentHistories.Add(new AcademyContentHistoryEntity
        {
            Id = Guid.NewGuid(), ContentJson = json, Reason = reason, ActorUserId = actor, CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return new(true, "Đã cập nhật nội dung Học viện KOC và lưu lịch sử thay đổi.");
    }

    public async Task<IReadOnlyList<AcademyLandingVersionItem>> GetHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.AcademyContentHistories.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(30)
                .Select(x => new AcademyLandingVersionItem(x.Id, x.Reason, x.ActorUserId, x.CreatedAtUtc)).ToListAsync(ct);
        }
        catch
        {
            return [];
        }
    }

    private static AcademyLandingContent Normalize(AcademyLandingContent x) => x with
    {
        Eyebrow = x.Eyebrow.Trim(), HeroTitle = x.HeroTitle.Trim(), HeroHighlight = x.HeroHighlight.Trim(),
        HeroDescription = x.HeroDescription.Trim(), HeroImageUrl = x.HeroImageUrl.Trim(),
        PrimaryCtaText = x.PrimaryCtaText.Trim(), PrimaryCtaUrl = x.PrimaryCtaUrl.Trim(),
        SecondaryCtaText = x.SecondaryCtaText.Trim(), SecondaryCtaUrl = x.SecondaryCtaUrl.Trim(),
        Roadmap = x.Roadmap.OrderBy(i => i.DisplayOrder).ToArray(),
        TrustItems = x.TrustItems.OrderBy(i => i.DisplayOrder).ToArray(),
        QualityItems = x.QualityItems.OrderBy(i => i.DisplayOrder).ToArray(),
        Outcomes = x.Outcomes.OrderBy(i => i.DisplayOrder).ToArray(),
        Resources = x.Resources.OrderBy(i => i.DisplayOrder).ToArray(),
        Stats = x.Stats.OrderBy(i => i.DisplayOrder).ToArray()
    };

    private static AcademyLandingSaveResult Fail(string message) => new(false, message);
}
