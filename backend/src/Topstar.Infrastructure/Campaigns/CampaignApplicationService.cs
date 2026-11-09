using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Campaigns;

public sealed class CampaignApplicationService(TopstarDbContext db) : ICampaignApplicationService
{
    public async Task<CampaignResult> ApplyAsync(Guid campaignId, Guid kocUserId, string? pitchNote, decimal? proposedAmount, CancellationToken cancellationToken = default)
    {
        if (proposedAmount is <= 0 or > 1_000_000_000m) return new(false, "Mức giá đề xuất phải lớn hơn 0 và không vượt quá 1 tỷ đồng.");
        var kocId = await db.KocProfiles.Where(x => x.UserId == kocUserId && x.Status == "Approved").Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!kocId.HasValue) return new(false, "Hồ sơ KOC chưa được duyệt.");
        var campaign = await db.Campaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null || !CampaignTransitions.CanApply(campaign.Status) || !campaign.IsVisible) return new(false, "Chiến dịch không còn nhận đăng ký.");
        var existing = await db.Applications.FirstOrDefaultAsync(x => x.CampaignId == campaignId && x.KocProfileId == kocId.Value, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            if (existing.Status != ApplicationStatuses.Withdrawn)
                return new(false, "Bạn đã đăng ký chiến dịch này.", existing.Id);
            existing.Status = ApplicationStatuses.Applied;
            existing.PitchNote = pitchNote?.Trim();
            existing.ProposedAmount = proposedAmount;
            existing.AppliedAtUtc = now;
            existing.UpdatedAtUtc = now;
            AddHistory(existing.Id, ApplicationStatuses.Withdrawn, ApplicationStatuses.Applied, "Reapply", kocUserId, now);
            AddAudit(kocUserId, existing.Id, "Reapply", new { campaignId });
            await db.SaveChangesAsync(cancellationToken);
            return new(true, "Đăng ký lại chiến dịch thành công.", existing.Id);
        }
        var application = new ApplicationEntity { Id = Guid.NewGuid(), CampaignId = campaignId, KocProfileId = kocId.Value, Status = ApplicationStatuses.Applied, PitchNote = pitchNote?.Trim(), ProposedAmount = proposedAmount, AppliedAtUtc = now, CreatedAtUtc = now };
        db.Applications.Add(application);
        AddHistory(application.Id, null, ApplicationStatuses.Applied, "Apply", kocUserId, now);
        AddAudit(kocUserId, application.Id, "Apply", new { campaignId });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Đăng ký đã tồn tại. Vui lòng tải lại trang.");
        }
        return new(true, "Đăng ký tham gia chiến dịch thành công.", application.Id);
    }

    public async Task<CampaignResult> WithdrawAsync(Guid applicationId, Guid kocUserId, CancellationToken cancellationToken = default)
    {
        var application = await (from a in db.Applications join k in db.KocProfiles on a.KocProfileId equals k.Id where a.Id == applicationId && k.UserId == kocUserId select a).FirstOrDefaultAsync(cancellationToken);
        if (application is null) return new(false, "Không tìm thấy đăng ký hoặc bạn không có quyền thao tác.");
        if (application.Status != ApplicationStatuses.Applied) return new(false, "Chỉ được rút đăng ký đang ở trạng thái Applied.");
        var now = DateTime.UtcNow;
        application.Status = ApplicationStatuses.Withdrawn; application.UpdatedAtUtc = now;
        AddHistory(application.Id, ApplicationStatuses.Applied, ApplicationStatuses.Withdrawn, "Withdraw", kocUserId, now);
        AddAudit(kocUserId, application.Id, "Withdraw", new { application.CampaignId });
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã rút đăng ký.", application.Id);
    }

    public async Task<IReadOnlyList<KocApplicationItem>> GetMineAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var query = from a in db.Applications.AsNoTracking()
                  join k in db.KocProfiles.AsNoTracking() on a.KocProfileId equals k.Id
                  join c in db.Campaigns.AsNoTracking() on a.CampaignId equals c.Id
                  join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                  where k.UserId == kocUserId
                  select new { Application = a, Campaign = c, Brand = b };

        if (window is not null)
        {
            window.Normalize();
            window.Total = await query.CountAsync(cancellationToken);
            window.Counts = await query.GroupBy(x => x.Application.Status)
                .Select(x => new { x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
            if (!string.IsNullOrWhiteSpace(window.Search))
            {
                var keyword = window.Search;
                query = query.Where(x => x.Campaign.Title.Contains(keyword) || x.Brand.CompanyName.Contains(keyword));
            }
            if (!string.IsNullOrWhiteSpace(window.Status))
                query = query.Where(x => x.Application.Status == window.Status);
            window.Filtered = await query.CountAsync(cancellationToken);
            window.Page = Math.Min(window.Page, window.Pages);
        }

        var ordered = query.OrderByDescending(x => x.Application.AppliedAtUtc).ThenBy(x => x.Application.Id);
        var pageQuery = window is null ? ordered : ordered.Skip((window.Page - 1) * window.Size).Take(window.Size);
        var items = await pageQuery.Select(x => new KocApplicationItem(x.Application.Id, x.Campaign.Id, x.Campaign.Title, x.Brand.CompanyName, x.Campaign.BudgetAmount, x.Application.ProposedAmount, x.Application.Status, x.Application.AppliedAtUtc)).ToListAsync(cancellationToken);
        var ids = items.Select(x => x.Id).ToArray();
        var histories = await db.ApplicationStatusHistories.AsNoTracking().Where(x => ids.Contains(x.ApplicationId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(x => x with { History = histories.Where(h => h.ApplicationId == x.Id).Select(h => new LifecycleEvent(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList() }).ToList();
    }

    private void AddAudit(Guid actor, Guid id, string action, object payload) => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "Application", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = DateTime.UtcNow });
    private void AddHistory(Guid id, string? from, string to, string? note, Guid actor, DateTime now) => db.ApplicationStatusHistories.Add(new ApplicationStatusHistoryEntity { Id = Guid.NewGuid(), ApplicationId = id, FromStatus = from, ToStatus = to, Note = note, ActorUserId = actor, CreatedAtUtc = now });
}
