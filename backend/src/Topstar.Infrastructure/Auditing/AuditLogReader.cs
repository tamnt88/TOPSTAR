using Microsoft.EntityFrameworkCore;
using Topstar.Application.Auditing;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Auditing;

public sealed class AuditLogReader(TopstarDbContext db) : IAuditLogReader
{
    public async Task<AuditLogPage> SearchAsync(AuditLogQuery request, CancellationToken ct = default)
    {
        var entityNames = await db.AuditLogs.AsNoTracking().Select(x => x.EntityName).Distinct()
            .OrderBy(x => x).Take(100).ToListAsync(ct);
        var actionNames = await db.AuditLogs.AsNoTracking().Select(x => x.ActionName).Distinct()
            .OrderBy(x => x).Take(100).ToListAsync(ct);
        var keyword = Clean(request.Keyword, 120);
        var entity = Clean(request.EntityName, 100);
        var action = Clean(request.ActionName, 100);
        var page = Math.Max(1, request.Page);
        var from = request.FromUtc;
        var to = request.ToUtc;
        if (from.HasValue && to.HasValue && from > to) (from, to) = (to, from);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.AddDays(1));
        if (entity is not null) query = query.Where(x => x.EntityName == entity);
        if (action is not null) query = query.Where(x => x.ActionName == action);
        if (keyword is not null)
            query = query.Where(x => x.EntityId.Contains(keyword) ||
                (x.ActorEmailSnapshot != null && x.ActorEmailSnapshot.Contains(keyword)) ||
                (x.ActorUserId != null && db.Users.Any(u => u.Id == x.ActorUserId && u.Email.Contains(keyword))));

        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / 50d));
        page = Math.Min(page, totalPages);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 50).Take(50)
            .Select(x => new AuditLogItem(x.Id,
                x.ActorEmailSnapshot ?? (x.ActorUserId == null ? "System" : db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.Email).FirstOrDefault() ?? "Unknown"),
                x.ActorRoleSnapshot ?? "LEGACY",
                x.EntityName, x.EntityId, x.ActionName, x.CreatedAtUtc))
            .ToListAsync(ct);
        return new AuditLogPage(items, total, entityNames, actionNames, page, totalPages);
    }

    public async Task<IReadOnlyList<AuditLogItem>> ExportAsync(AuditLogQuery request, CancellationToken ct = default)
    {
        var keyword = Clean(request.Keyword, 120);
        var entity = Clean(request.EntityName, 100);
        var action = Clean(request.ActionName, 100);
        var from = request.FromUtc; var to = request.ToUtc;
        if (from.HasValue && to.HasValue && from > to) (from, to) = (to, from);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.AddDays(1));
        if (entity is not null) query = query.Where(x => x.EntityName == entity);
        if (action is not null) query = query.Where(x => x.ActionName == action);
        if (keyword is not null) query = query.Where(x => x.EntityId.Contains(keyword) ||
            (x.ActorEmailSnapshot != null && x.ActorEmailSnapshot.Contains(keyword)) ||
            (x.ActorUserId != null && db.Users.Any(u => u.Id == x.ActorUserId && u.Email.Contains(keyword))));
        return await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Take(5000)
            .Select(x => new AuditLogItem(x.Id,
                x.ActorEmailSnapshot ?? (x.ActorUserId == null ? "System" : db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.Email).FirstOrDefault() ?? "Unknown"),
                x.ActorRoleSnapshot ?? "LEGACY", x.EntityName, x.EntityId, x.ActionName, x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    private static string? Clean(string? value, int max)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, max)];
    }
}
