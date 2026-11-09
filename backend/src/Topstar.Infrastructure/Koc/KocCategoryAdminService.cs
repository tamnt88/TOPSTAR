using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Koc;

internal sealed class KocCategoryAdminService(TopstarDbContext db) : IKocCategoryAdminService
{
    public async Task<IReadOnlyList<KocCategoryAdminItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await db.KocCategories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var socialUsage = await db.KocSocialAccountCategories.AsNoTracking().GroupBy(x => x.CategoryId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        var profileUsage = await db.KocProfileCategories.AsNoTracking().GroupBy(x => x.CategoryId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        var campaignUsage = await db.CampaignTargetCategories.AsNoTracking().GroupBy(x => x.CategoryId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        return categories.Select(category => new KocCategoryAdminItem(category.Id, category.Code, category.Name, category.SortOrder,
            category.IsActive, socialUsage.GetValueOrDefault(category.Id) + profileUsage.GetValueOrDefault(category.Id) + campaignUsage.GetValueOrDefault(category.Id),
            category.CreatedAtUtc, category.UpdatedAtUtc)).ToList();
    }

    public async Task<KocCategoryAdminItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => (await GetAllAsync(cancellationToken)).FirstOrDefault(x => x.Id == id);

    public async Task<IReadOnlyList<KocCategoryHistoryItem>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (from history in db.KocCategoryHistories.AsNoTracking()
                          join user in db.Users.AsNoTracking() on history.ActorUserId equals user.Id
                          orderby history.CreatedAtUtc descending, history.Id descending
                          select new { History = history, user.Email }).ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var previous = ReadSnapshot(row.History.PreviousJson);
            var current = ReadSnapshot(row.History.CurrentJson);
            var snapshot = current ?? previous;
            return new KocCategoryHistoryItem(row.History.Id, row.History.KocCategoryId, snapshot?.Name ?? "Danh mục đã xóa",
                snapshot?.Code ?? "—", row.History.ActionName, ActionLabel(row.History.ActionName), row.History.Reason,
                row.Email, row.History.CreatedAtUtc, Summary(previous), Summary(current));
        }).ToList();
    }

    public async Task<KocCategoryAdminResult> SaveAsync(Guid actorUserId, SaveKocCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var code = KocCategoryAdminRules.NormalizeCode(request.Code);
        var name = request.Name?.Trim() ?? string.Empty;
        var reason = request.Reason?.Trim() ?? string.Empty;
        var validation = KocCategoryAdminRules.Validate(code, name, request.SortOrder, reason);
        if (validation is not null) return new(false, validation, request.Id);

        if (await db.KocCategories.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken))
            return new(false, "Mã danh mục đã tồn tại.", request.Id, "Code");
        if (await db.KocCategories.AnyAsync(x => x.Name == name && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken))
            return new(false, "Tên lĩnh vực đã tồn tại.", request.Id, "Name");

        var now = DateTime.UtcNow;
        KocCategoryEntity category;
        object? previous = null;
        var action = "Create";
        if (request.Id.HasValue)
        {
            var existing = await db.KocCategories.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
            if (existing is null) return new(false, "Danh mục không còn tồn tại.", request.Id);
            category = existing;
            previous = Snapshot(category);
            action = "Update";
            category.Code = code;
            category.Name = name;
            category.SortOrder = request.SortOrder;
            category.UpdatedAtUtc = now;
        }
        else
        {
            category = new KocCategoryEntity { Id = Guid.NewGuid(), Code = code, Name = name, SortOrder = request.SortOrder, IsActive = true, CreatedAtUtc = now };
            db.KocCategories.Add(category);
        }

        AddHistoryAndAudit(category, action, previous, Snapshot(category), reason, actorUserId, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, request.Id.HasValue ? "Đã cập nhật lĩnh vực nội dung." : "Đã thêm lĩnh vực nội dung.", category.Id);
    }

    public async Task<KocCategoryAdminResult> SetActiveAsync(Guid actorUserId, Guid id, bool isActive, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500) return new(false, "Vui lòng nhập lý do thay đổi từ 10 đến 500 ký tự.", id, "Reason");
        var category = await db.KocCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new(false, "Danh mục không còn tồn tại.", id);
        if (category.IsActive == isActive) return new(true, isActive ? "Danh mục đang hiển thị." : "Danh mục đang tạm ẩn.", id);

        var previous = Snapshot(category);
        category.IsActive = isActive;
        category.UpdatedAtUtc = DateTime.UtcNow;
        AddHistoryAndAudit(category, isActive ? "Activate" : "Deactivate", previous, Snapshot(category), reason, actorUserId, category.UpdatedAtUtc.Value);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, isActive ? "Đã hiển thị lại lĩnh vực nội dung." : "Đã ẩn lĩnh vực khỏi các lựa chọn mới. Dữ liệu lịch sử vẫn được giữ nguyên.", id);
    }

    public async Task<KocCategoryAdminResult> DeleteAsync(Guid actorUserId, Guid id, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        var validation = KocCategoryAdminRules.ValidateDeleteReason(reason);
        if (validation is not null) return new(false, validation, id, "Reason");

        var category = await db.KocCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new(false, "Danh mục không còn tồn tại.", id);

        var usageCount = await db.KocProfileCategories.CountAsync(x => x.CategoryId == id, cancellationToken)
            + await db.KocSocialAccountCategories.CountAsync(x => x.CategoryId == id, cancellationToken)
            + await db.CampaignTargetCategories.CountAsync(x => x.CategoryId == id, cancellationToken);
        if (usageCount > 0)
            return new(false, $"Không thể xóa vì lĩnh vực đang được sử dụng tại {usageCount} hồ sơ hoặc chiến dịch. Hãy tạm ẩn danh mục để ngăn lựa chọn mới.", id);

        var now = DateTime.UtcNow;
        var snapshot = Snapshot(category);
        db.KocCategoryHistories.Add(new() { KocCategoryId = null, ActionName = "Delete", PreviousJson = JsonSerializer.Serialize(snapshot), CurrentJson = "null", Reason = reason, ActorUserId = actorUserId, CreatedAtUtc = now });
        db.AuditLogs.Add(new() { ActorUserId = actorUserId, EntityName = "KocCategory", EntityId = category.Id.ToString(), ActionName = "Delete", PayloadJson = JsonSerializer.Serialize(new { Previous = snapshot, Current = (object?)null, Reason = reason }), CreatedAtUtc = now });
        db.KocCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã xóa lĩnh vực nội dung chưa được sử dụng.", id);
    }

    private void AddHistoryAndAudit(KocCategoryEntity category, string action, object? previous, object current, string reason, Guid actor, DateTime now)
    {
        var previousJson = JsonSerializer.Serialize(previous);
        var currentJson = JsonSerializer.Serialize(current);
        db.KocCategoryHistories.Add(new() { KocCategoryId = category.Id, ActionName = action, PreviousJson = previousJson, CurrentJson = currentJson, Reason = reason, ActorUserId = actor, CreatedAtUtc = now });
        db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "KocCategory", EntityId = category.Id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(new { Previous = previous, Current = current, Reason = reason }), CreatedAtUtc = now });
    }

    private static object Snapshot(KocCategoryEntity x) => new { x.Code, x.Name, x.SortOrder, x.IsActive };

    private static CategorySnapshot? ReadSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
        try { return JsonSerializer.Deserialize<CategorySnapshot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return null; }
    }

    private static string Summary(CategorySnapshot? value)
        => value is null ? "—" : $"{value.Name} · {value.Code} · thứ tự {value.SortOrder} · {(value.IsActive ? "Đang hiển thị" : "Tạm ẩn")}";

    private static string ActionLabel(string action) => action switch
    {
        "Create" => "Thêm mới", "Update" => "Cập nhật", "Activate" => "Hiển thị lại",
        "Deactivate" => "Tạm ẩn", "Delete" => "Xóa", _ => action
    };

    private sealed record CategorySnapshot(string Code, string Name, int SortOrder, bool IsActive);
}
