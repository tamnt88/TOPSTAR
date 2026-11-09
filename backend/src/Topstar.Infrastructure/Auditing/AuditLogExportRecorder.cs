using System.Text.Json;
using Topstar.Application.Auditing;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Auditing;

public sealed class AuditLogExportRecorder(TopstarDbContext db) : IAuditLogExportRecorder
{
    public async Task RecordAsync(Guid actorUserId, AuditLogQuery query, int exportedCount,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLogEntity
        {
            ActorUserId = actorUserId,
            EntityName = "AuditLog",
            EntityId = "CSV",
            ActionName = "Export",
            PayloadJson = JsonSerializer.Serialize(new
            {
                Keyword = Limit(query.Keyword, 120),
                EntityName = Limit(query.EntityName, 100),
                ActionName = Limit(query.ActionName, 100),
                query.FromUtc,
                query.ToUtc,
                ExportedCount = exportedCount,
                Limit = 5000
            }),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private static string? Limit(string? value, int max)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, max)];
    }
}
