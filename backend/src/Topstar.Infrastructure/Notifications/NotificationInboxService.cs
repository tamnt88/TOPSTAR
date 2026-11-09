using System.Data;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Notifications;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Notifications;

public sealed class NotificationInboxService(TopstarDbContext db) : INotificationInboxService
{
    public Task<int> GetUnreadCountAsync(Guid userId, string role, CancellationToken ct = default)
    {
        role = NormalizeRole(role);
        return db.Notifications.AsNoTracking().CountAsync(x => x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible && !x.IsRead, ct);
    }

    public async Task<NotificationPage> GetPageAsync(Guid userId, string role, string? filter, string? search, int start, int length, CancellationToken ct = default, string? category = null)
    {
        role = NormalizeRole(role);
        if (!await HasRoleAsync(userId, role, ct)) return new(0, 0, 0, 0, 0, []);
        var all = db.Notifications.AsNoTracking().Where(x => x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role));
        var active = await all.CountAsync(x => x.IsVisible, ct);
        var archived = await all.CountAsync(x => !x.IsVisible, ct);
        var unread = await all.CountAsync(x => x.IsVisible && !x.IsRead, ct);
        var query = filter?.Trim().ToLowerInvariant() switch
        {
            "archived" => all.Where(x => !x.IsVisible),
            "unread" => all.Where(x => x.IsVisible && !x.IsRead),
            "read" => all.Where(x => x.IsVisible && x.IsRead),
            _ => all.Where(x => x.IsVisible)
        };
        var total = await query.CountAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            if (search.Length > 120) search = search[..120];
            query = query.Where(x => x.Title.Contains(search) || x.Body.Contains(search));
        }
        var categoryTypes = NotificationCategoryTypes(category);
        if (categoryTypes.Length > 0) query = query.Where(x => categoryTypes.Contains(x.Type));
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip(Math.Max(0, start)).Take(Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length)).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var events = await db.NotificationEvents.AsNoTracking().Where(x => ids.Contains(x.NotificationId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var map = events.GroupBy(x => x.NotificationId).ToDictionary(g => g.Key, g => (IReadOnlyList<NotificationEventItem>)g.Select(e => new NotificationEventItem(e.EventType, e.CreatedAtUtc, e.TargetUrlSnapshot, e.ActorRoleSnapshot)).ToList());
        return new(total, count, unread, active, archived, rows.Select(x => new NotificationItem(x.Id, x.Type, x.Title, x.Body, x.IsRead, x.IsVisible, x.ReadAtUtc, x.HiddenAtUtc, x.CreatedAtUtc, PreviewTarget(x, role), x.SourceEntityType, x.SourceEntityId, map.GetValueOrDefault(x.Id, []))).ToList());
    }

    public async Task<NotificationInbox> GetInboxAsync(Guid userId, string role, string? filter, CancellationToken ct = default)
    {
        role = NormalizeRole(role);
        if (!await HasRoleAsync(userId, role, ct)) return new(0, []);
        filter = (filter ?? "all").Trim().ToLowerInvariant();
        var query = db.Notifications.AsNoTracking().Where(x => x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role));
        query = filter switch
        {
            "archived" => query.Where(x => !x.IsVisible),
            "unread" => query.Where(x => x.IsVisible && !x.IsRead),
            _ => query.Where(x => x.IsVisible)
        };
        var rows = await query.OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var events = await db.NotificationEvents.AsNoTracking().Where(x => ids.Contains(x.NotificationId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var map = events.GroupBy(x => x.NotificationId).ToDictionary(g => g.Key, g => (IReadOnlyList<NotificationEventItem>)g.Select(e => new NotificationEventItem(e.EventType, e.CreatedAtUtc, e.TargetUrlSnapshot, e.ActorRoleSnapshot)).ToList());
        var unread = await db.Notifications.AsNoTracking().CountAsync(x => x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible && !x.IsRead, ct);
        return new(unread, rows.Select(x => new NotificationItem(x.Id, x.Type, x.Title, x.Body, x.IsRead, x.IsVisible, x.ReadAtUtc, x.HiddenAtUtc, x.CreatedAtUtc, PreviewTarget(x, role), x.SourceEntityType, x.SourceEntityId, map.GetValueOrDefault(x.Id, []))).ToList());
    }

    public async Task<NotificationResult> MarkReadAsync(Guid userId, string role, Guid id, CancellationToken ct = default)
    {
        role = NormalizeRole(role); if (!await HasRoleAsync(userId, role, ct)) return new(false, "Bạn không có quyền thao tác portal này."); var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var affected = await db.Notifications.Where(x => x.Id == id && x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible && !x.IsRead).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);
        if (affected == 0)
        {
            var exists = await db.Notifications.AsNoTracking().AnyAsync(x => x.Id == id && x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible, ct);
            return exists ? new(true, "Thông báo đã được đọc trước đó.") : new(false, "Không tìm thấy thông báo.");
        }
        AddEvent(id, "Read", userId, role, now);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(true, "Đã đánh dấu đã đọc.");
    }

    public async Task<NotificationResult> MarkAllReadAsync(Guid userId, string role, CancellationToken ct = default)
    {
        role = NormalizeRole(role); if (!await HasRoleAsync(userId, role, ct)) return new(false, "Bạn không có quyền thao tác portal này."); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ids = await db.Notifications.AsNoTracking().Where(x => x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible && !x.IsRead).Select(x => x.Id).ToListAsync(ct);
        if (ids.Count == 0) return new(true, "Không còn thông báo chưa đọc.");
        var now = DateTime.UtcNow;
        var affected = await db.Notifications.Where(x => ids.Contains(x.Id) && x.UserId == userId && !x.IsRead).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);
        foreach (var id in ids) AddEvent(id, "Read", userId, role, now);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(true, $"Đã đánh dấu {affected} thông báo là đã đọc.");
    }

    public Task<NotificationResult> ArchiveAsync(Guid userId, string role, Guid id, CancellationToken ct = default) => SetVisibility(userId, role, id, false, ct);
    public Task<NotificationResult> RestoreAsync(Guid userId, string role, Guid id, CancellationToken ct = default) => SetVisibility(userId, role, id, true, ct);

    public async Task<NotificationOpenResult> OpenAsync(Guid userId, string role, Guid id, CancellationToken ct = default)
    {
        role = NormalizeRole(role);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await HasRoleAsync(userId, role, ct)) return new(false, "Bạn không có quyền mở portal này.", null);
        var item = await db.Notifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible, ct);
        if (item is null) return new(false, "Không tìm thấy thông báo đang hoạt động.", null);
        var target = await ResolveTargetAsync(item, role, ct);
        if (target is null) return new(false, "Thông báo này chưa có trang công việc phù hợp.", null);
        var now = DateTime.UtcNow;
        if (!item.IsRead)
        {
            var affected = await db.Notifications.Where(x => x.Id == id && x.UserId == userId && x.IsVisible && !x.IsRead).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);
            if (affected == 1) AddEvent(id, "Read", userId, role, now);
        }
        AddEvent(id, "Opened", userId, role, now, target);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(true, "Đang mở công việc.", target);
    }

    private async Task<NotificationResult> SetVisibility(Guid userId, string role, Guid id, bool visible, CancellationToken ct)
    {
        role = NormalizeRole(role); if (!await HasRoleAsync(userId, role, ct)) return new(false, "Bạn không có quyền thao tác portal này."); var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var affected = await db.Notifications.Where(x => x.Id == id && x.UserId == userId && (x.AudienceRole == null || x.AudienceRole == role) && x.IsVisible != visible).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsVisible, visible).SetProperty(x => x.HiddenAtUtc, visible ? null : now).SetProperty(x => x.UpdatedAtUtc, now), ct);
        if (affected == 0) return new(false, "Thông báo không tồn tại hoặc trạng thái đã thay đổi.");
        AddEvent(id, visible ? "Restored" : "Hidden", userId, role, now);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(true, visible ? "Đã khôi phục thông báo." : "Đã lưu trữ thông báo.");
    }

    private async Task<string?> ResolveTargetAsync(NotificationEntity item, string role, CancellationToken ct)
    {
        var explicitTarget = SafeTarget(item.TargetUrl);
        if (explicitTarget is not null) return explicitTarget;
        if (!item.SourceEntityId.HasValue) return FallbackTarget(item.Type, role);
        var id = item.SourceEntityId.Value;
        switch (item.SourceEntityType)
        {
            case "DisputeCase": return $"/{RoleSegment(role)}/DisputeDetail?id={id:D}";
            case "Campaign": return role switch { "ADMIN" => $"/Admin/CampaignReview?id={id:D}", "BRAND" => $"/Brand/CampaignEdit?id={id:D}", "KOC" => $"/Koc/CampaignDetail?id={id:D}", _ => null };
            case "Shortlist" when role == "BRAND":
                var shortlistCampaign = await db.Shortlists.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.CampaignId).FirstOrDefaultAsync(ct);
                return shortlistCampaign.HasValue ? $"/Brand/Shortlist?id={shortlistCampaign.Value:D}" : null;
            case "Submission" when role == "BRAND":
                var submissionCampaign = await (from s in db.Submissions.AsNoTracking() join p in db.Participations.AsNoTracking() on s.ParticipationId equals p.Id where s.Id == id select (Guid?)p.CampaignId).FirstOrDefaultAsync(ct);
                return submissionCampaign.HasValue ? $"/Brand/Deliverables?id={submissionCampaign.Value:D}" : null;
            case "Submission" when role == "KOC": return $"/Koc/Tasks?focus={id:D}";
            case "Schedule" when role == "KOC": return $"/Koc/Bookings#booking-{id:D}";
            case "Shipment" when role == "KOC": return $"/Koc/Shipments?focus={id:D}";
            case "Review" when role == "KOC": return "/Koc/Reviews";
            case "Participation" when role == "KOC":
                var participationCampaign = await db.Participations.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.CampaignId).FirstOrDefaultAsync(ct);
                return participationCampaign.HasValue ? $"/Koc/CampaignDetail?id={participationCampaign.Value:D}" : null;
            case "Application" when role == "KOC":
                var applicationCampaign = await db.Applications.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.CampaignId).FirstOrDefaultAsync(ct);
                return applicationCampaign.HasValue ? $"/Koc/Jobs?section=applications&focusApplication={id:D}#application-{id:D}" : null;
            default: return FallbackTarget(item.Type, role);
        }
    }

    private static string? PreviewTarget(NotificationEntity item, string role) => SafeTarget(item.TargetUrl) ?? (item.SourceEntityType switch
    {
        "DisputeCase" when item.SourceEntityId.HasValue => $"/{RoleSegment(role)}/DisputeDetail?id={item.SourceEntityId.Value:D}",
        "Campaign" when item.SourceEntityId.HasValue => role switch { "ADMIN" => $"/Admin/CampaignReview?id={item.SourceEntityId.Value:D}", "BRAND" => $"/Brand/CampaignEdit?id={item.SourceEntityId.Value:D}", "KOC" => $"/Koc/CampaignDetail?id={item.SourceEntityId.Value:D}", _ => null },
        "Submission" when role == "KOC" && item.SourceEntityId.HasValue => $"/Koc/Tasks?focus={item.SourceEntityId.Value:D}",
        "Schedule" when role == "KOC" && item.SourceEntityId.HasValue => $"/Koc/Bookings#booking-{item.SourceEntityId.Value:D}",
        "Shipment" when role == "KOC" && item.SourceEntityId.HasValue => $"/Koc/Shipments?focus={item.SourceEntityId.Value:D}",
        "Application" when role == "KOC" && item.SourceEntityId.HasValue => $"/Koc/Jobs?section=applications&focusApplication={item.SourceEntityId.Value:D}#application-{item.SourceEntityId.Value:D}",
        "Review" when role == "KOC" => "/Koc/Reviews",
        _ => FallbackTarget(item.Type, role)
    });
    private static string[] NotificationCategoryTypes(string? category) => category?.Trim().ToLowerInvariant() switch
    {
        "work" => ["Participation","CampaignCompleted","SubmissionReview","SubmissionOverdue","Shipment","Schedule","CampaignCancelled"],
        "finance" => ["Wallet","WalletRelease","Payout"],
        "support" => ["Dispute","DisputeMessage","CampaignReview","ReviewModeration","Violation"],
        _ => []
    };
    private Task<bool> HasRoleAsync(Guid userId, string role, CancellationToken ct) => (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id where ur.UserId == userId && r.Code.ToUpper() == role select ur.UserId).AnyAsync(ct);
    private void AddEvent(Guid id, string type, Guid actor, string role, DateTime now, string? target = null) => db.NotificationEvents.Add(new() { Id = Guid.NewGuid(), NotificationId = id, EventType = type, ActorUserId = actor, ActorRoleSnapshot = role, TargetUrlSnapshot = target, CreatedAtUtc = now });
    private static string NormalizeRole(string role) => (role ?? "").Trim().ToUpperInvariant();
    private static string RoleSegment(string role) => role switch { "ADMIN" => "Admin", "BRAND" => "Brand", _ => "Koc" };
    private static string? SafeTarget(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 500 && value[0] == '/' && (value.Length == 1 || value[1] != '/') && !value.Contains('\\') && !value.Any(char.IsControl) ? value : null;
    private static string? FallbackTarget(string type, string role) => (role, type) switch { ("ADMIN", "Dispute") or ("ADMIN", "DisputeMessage") => "/Admin/Disputes", ("ADMIN", "Operations") => "/Admin/Operations", ("ADMIN", "ReviewModeration") => "/Admin/ReviewModeration", ("BRAND", "Shortlist") => "/Brand/Shortlist", ("BRAND", "Submission") or ("BRAND", "SubmissionOverdue") => "/Brand/Deliverables", ("BRAND", "Shipment") => "/Brand/Shipments", ("BRAND", "Schedule") => "/Brand/Bookings", ("BRAND", "Dispute") or ("BRAND", "DisputeMessage") or ("BRAND", "CampaignCancelled") => "/Brand/Disputes", ("BRAND", "CampaignReview") or ("BRAND", "ReviewModeration") => "/Brand/Reviews", ("BRAND", "Violation") => "/Brand/Profile", ("KOC", "Participation") or ("KOC", "CampaignCompleted") => "/Koc/Jobs", ("KOC", "SubmissionReview") or ("KOC", "SubmissionOverdue") => "/Koc/Tasks", ("KOC", "Shipment") => "/Koc/Shipments", ("KOC", "Schedule") => "/Koc/Bookings", ("KOC", "Dispute") or ("KOC", "DisputeMessage") or ("KOC", "CampaignCancelled") => "/Koc/Disputes", ("KOC", "Wallet") or ("KOC", "WalletRelease") or ("KOC", "Payout") => "/Koc/Wallet", ("KOC", "CampaignReview") or ("KOC", "ReviewModeration") => "/Koc/Reviews", ("KOC", "Violation") => "/Koc/Profile", _ => null };
}
