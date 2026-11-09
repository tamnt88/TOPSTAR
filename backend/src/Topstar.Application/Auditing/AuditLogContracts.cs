namespace Topstar.Application.Auditing;

public static class AuditCsvRules
{
    public static string Escape(string? value)
    {
        value = (value ?? "").Replace("\r", " ").Replace("\n", " ");
        var first = value.TrimStart().FirstOrDefault();
        if (first != default && "=+-@".Contains(first)) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

public sealed record AuditLogQuery(string? Keyword, string? EntityName, string? ActionName,
    DateTime? FromUtc, DateTime? ToUtc, int Page = 1);
public sealed record AuditLogItem(long Id, string Actor, string ActorRole, string EntityName, string EntityId,
    string ActionName, DateTime CreatedAtUtc);
public sealed record AuditLogPage(IReadOnlyList<AuditLogItem> Items, int TotalCount,
    IReadOnlyList<string> EntityNames, IReadOnlyList<string> ActionNames, int CurrentPage, int TotalPages);

public interface IAuditLogReader
{
    Task<AuditLogPage> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogItem>> ExportAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}

public interface IAuditLogExportRecorder
{
    Task RecordAsync(Guid actorUserId, AuditLogQuery query, int exportedCount,
        CancellationToken cancellationToken = default);
}
