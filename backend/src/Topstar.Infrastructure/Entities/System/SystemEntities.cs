namespace Topstar.Infrastructure.Entities;

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmailSnapshot { get; set; }
    public string? ActorRoleSnapshot { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? HiddenAtUtc { get; set; }
    public string? TargetUrl { get; set; }
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? AudienceRole { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class NotificationEventEntity
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? TargetUrlSnapshot { get; set; }
    public string? ActorRoleSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class OperationalEscalationEntity
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? DisputeCaseId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime FirstDetectedAtUtc { get; set; }
    public DateTime LastDetectedAtUtc { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class OperationalEscalationHistoryEntity
{
    public Guid Id { get; set; }
    public Guid EscalationId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class OperationalScanRunEntity
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public int DetectedCount { get; set; }
    public int CreatedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int CriticalCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class OperationalSlaPolicyEntity
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public int HighAfterHours { get; set; }
    public int CriticalAfterHours { get; set; }
    public bool IsActive { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class OperationalSlaPolicyHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public int? FromHighAfterHours { get; set; }
    public int? FromCriticalAfterHours { get; set; }
    public bool? FromIsActive { get; set; }
    public int ToHighAfterHours { get; set; }
    public int ToCriticalAfterHours { get; set; }
    public bool ToIsActive { get; set; }
    public string Note { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
