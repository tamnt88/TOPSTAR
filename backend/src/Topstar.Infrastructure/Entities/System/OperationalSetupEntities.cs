namespace Topstar.Infrastructure.Entities;

public sealed class OperationalRuntimePolicyEntity
{
    public Guid Id { get; set; }
    public int ReviewEditHours { get; set; }
    public int BookingLeadMinutes { get; set; }
    public int BookingMaxHours { get; set; }
    public int MaxPayoutAccounts { get; set; }
    public int OtpRequestLimit { get; set; }
    public int OtpWindowMinutes { get; set; }
    public int OtpExpiryMinutes { get; set; }
    public int OtpMaxAttempts { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class OperationalRuntimePolicyHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
public sealed class OperationalCatalogItemEntity
{
    public Guid Id { get; set; }
    public string CatalogType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultSeverity { get; set; }
    public int? DefaultPenaltyPoints { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class OperationalCatalogHistoryEntity
{
    public Guid Id { get; set; }
    public Guid CatalogItemId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
