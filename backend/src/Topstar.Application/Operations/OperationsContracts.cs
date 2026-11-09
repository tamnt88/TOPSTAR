using Topstar.Application.Schedules;
using Topstar.Application.Shipments;
namespace Topstar.Application.Operations;

public static class OperationalSlaRules
{
    public const int MaximumHours = 2160;
    public static bool IsValidThresholds(int highAfterHours,int criticalAfterHours)
        => highAfterHours>=0&&highAfterHours<=MaximumHours&&criticalAfterHours>highAfterHours&&criticalAfterHours<=MaximumHours;
    public static bool IsDue(DateTime occurredAtUtc,DateTime evaluatedAtUtc,int highAfterHours)
        => evaluatedAtUtc>=occurredAtUtc.AddHours(highAfterHours);
    public static string Severity(DateTime occurredAtUtc,DateTime evaluatedAtUtc,int criticalAfterHours)
        => evaluatedAtUtc>=occurredAtUtc.AddHours(criticalAfterHours)?"Critical":"High";
}

public sealed record ShipmentIssueAdminItem(Guid ShipmentId,string CampaignTitle,string BrandName,string KocName,string? Carrier,string? TrackingCode,string IssueNote,DateTime UpdatedAtUtc,IReadOnlyList<ShipmentHistoryItem> History,IReadOnlyList<ShipmentEvidenceItem> Evidence,IReadOnlyList<ShipmentTrackingItem> Tracking);
public sealed record ScheduleAttentionAdminItem(Guid ScheduleId,string CampaignTitle,string BrandName,string KocName,string Status,DateTime? StartsAtUtc,DateTime? EndsAtUtc,int RescheduleCount,string AttentionReason,IReadOnlyList<ScheduleHistoryItem> History);
public sealed record EscalationHistoryItem(string? FromStatus,string ToStatus,string? Note,string ActorLabel,DateTime CreatedAtUtc);
public sealed record SubmissionEscalationAdminItem(Guid EscalationId,Guid SubmissionId,string CampaignTitle,string BrandName,string KocName,string DeliverableType,DateTime DueDateUtc,string SubmissionStatus,string Severity,string EscalationStatus,DateTime FirstDetectedAtUtc,DateTime LastDetectedAtUtc,IReadOnlyList<EscalationHistoryItem> History);
public sealed record OperationalScanRunItem(DateTime StartedAtUtc,DateTime CompletedAtUtc,int DetectedCount,int CreatedCount,int ResolvedCount,int CriticalCount,string ActorLabel);
public sealed record DisputeEscalationAdminItem(Guid EscalationId,Guid DisputeCaseId,string Subject,string Category,string Priority,string DisputeStatus,string Severity,string EscalationStatus,DateTime LastDetectedAtUtc,DateTime FirstDetectedAtUtc,IReadOnlyList<EscalationHistoryItem> History);
public sealed record SlaPolicyHistoryItem(int? FromHighAfterHours,int? FromCriticalAfterHours,bool? FromIsActive,int ToHighAfterHours,int ToCriticalAfterHours,bool ToIsActive,string Note,string ActorLabel,DateTime CreatedAtUtc);
public sealed record SlaPolicyAdminItem(Guid Id,string RuleCode,string DisplayName,int HighAfterHours,int CriticalAfterHours,bool IsActive,DateTime EffectiveAtUtc,string ActorLabel,IReadOnlyList<SlaPolicyHistoryItem> History);
public sealed record OperationsAdminDashboard(IReadOnlyList<ShipmentIssueAdminItem> ShipmentIssues,IReadOnlyList<ScheduleAttentionAdminItem> ScheduleAttention,IReadOnlyList<SubmissionEscalationAdminItem> SubmissionEscalations,IReadOnlyList<OperationalScanRunItem> SubmissionScanRuns,IReadOnlyList<SubmissionEscalationAdminItem> BrandReviewEscalations,IReadOnlyList<OperationalScanRunItem> BrandReviewScanRuns,IReadOnlyList<DisputeEscalationAdminItem> DisputeEscalations,IReadOnlyList<OperationalScanRunItem> DisputeScanRuns,IReadOnlyList<SlaPolicyAdminItem> SlaPolicies);
public sealed record OperationsResult(bool Success,string Message);
public sealed record OperationalQueueCount(int Total,int Critical,int Open);
public interface IOperationsAdminService
{
    Task<OperationalQueueCount> GetQueueCountAsync(CancellationToken cancellationToken=default);
    Task<OperationsAdminDashboard> GetDashboardAsync(CancellationToken cancellationToken=default);
    Task<OperationsAdminDashboard> GetCampaignDashboardAsync(Guid campaignId,CancellationToken cancellationToken=default);
    Task<OperationsResult> ResolveShipmentAsync(Guid adminUserId,Guid shipmentId,string decision,string note,CancellationToken cancellationToken=default);
    Task<OperationsResult> CancelScheduleAsync(Guid adminUserId,Guid scheduleId,string reason,CancellationToken cancellationToken=default);
    Task<OperationsResult> ScanSubmissionOverdueAsync(Guid adminUserId,CancellationToken cancellationToken=default);
    Task<OperationsResult> AcknowledgeEscalationAsync(Guid adminUserId,Guid escalationId,string note,CancellationToken cancellationToken=default);
    Task<OperationsResult> ScanDisputeSlaAsync(Guid adminUserId,CancellationToken cancellationToken=default);
    Task<OperationsResult> ScanBrandReviewSlaAsync(Guid adminUserId,CancellationToken cancellationToken=default);
    Task<OperationsResult> UpdateSlaPolicyAsync(Guid adminUserId,Guid policyId,int highAfterHours,int criticalAfterHours,bool isActive,string note,CancellationToken cancellationToken=default);
}
