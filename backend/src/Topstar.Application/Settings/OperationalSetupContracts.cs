namespace Topstar.Application.Settings;

public static class OperationalCatalogTypes
{
    public const string Violation = "VIOLATION_CATEGORY";
    public const string Deliverable = "DELIVERABLE_TYPE";
    public const string ShipmentCheckpoint = "SHIPMENT_CHECKPOINT";
    public static readonly string[] All = [Violation, Deliverable, ShipmentCheckpoint];
}

public sealed record OperationalPolicyItem(int ReviewEditHours, int BookingLeadMinutes, int BookingMaxHours,
    int MaxPayoutAccounts, int OtpRequestLimit, int OtpWindowMinutes, int OtpExpiryMinutes, int OtpMaxAttempts,
    DateTime UpdatedAtUtc, IReadOnlyList<OperationalPolicyHistoryItem> History);
public sealed record OperationalPolicyHistoryItem(string SnapshotJson, string Reason, string Actor, DateTime CreatedAtUtc);
public sealed record OperationalCatalogItem(Guid Id, string CatalogType, string Code, string Name, string? Description,
    string? DefaultSeverity, int? DefaultPenaltyPoints, int SortOrder, bool IsActive, DateTime UpdatedAtUtc);
public sealed record OperationalCatalogHistoryItem(string Action,string SnapshotJson,string Reason,string Actor,DateTime CreatedAtUtc);
public sealed record OperationalSetupResult(bool Success, string Message, Guid? Id = null);

public interface IOperationalSetupService
{
    Task<OperationalPolicyItem> GetPolicyAsync(CancellationToken ct = default);
    Task<OperationalSetupResult> UpdatePolicyAsync(Guid actorId, int reviewEditHours, int bookingLeadMinutes,
        int bookingMaxHours, int maxPayoutAccounts, int otpRequestLimit, int otpWindowMinutes,
        int otpExpiryMinutes, int otpMaxAttempts, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<OperationalCatalogItem>> GetCatalogAsync(string? catalogType = null, bool activeOnly = false, CancellationToken ct = default);
    Task<OperationalCatalogItem?> FindCatalogAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OperationalCatalogHistoryItem>> GetCatalogHistoryAsync(CancellationToken ct = default);
    Task<OperationalSetupResult> SaveCatalogAsync(Guid actorId, Guid? id, string catalogType, string code, string name,
        string? description, string? defaultSeverity, int? defaultPenaltyPoints, int sortOrder, bool isActive,
        string reason, CancellationToken ct = default);
    Task<OperationalSetupResult> DeleteCatalogAsync(Guid actorId, Guid id, string reason, CancellationToken ct = default);
}
