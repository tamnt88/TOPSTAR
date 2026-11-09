namespace Topstar.Application.Disputes;
public static class DisputeStatuses{public const string Open="Open";public const string InReview="InReview";public const string Resolved="Resolved";public const string Rejected="Rejected";public const string Cancelled="Cancelled";}
public static class DisputeCategories{public static readonly string[] All=["Booking","Shipment","Deliverable","Payment","Review","Other"];public static bool IsValid(string value)=>All.Contains(value,StringComparer.OrdinalIgnoreCase);}
public static class DisputeTransitions{public static bool IsActive(string s)=>s is DisputeStatuses.Open or DisputeStatuses.InReview;public static bool CanTake(string s)=>s==DisputeStatuses.Open;public static bool CanResolve(string s)=>IsActive(s);public static bool CanCancel(string s)=>s==DisputeStatuses.Open;public static bool CanReleaseEarnings(string s)=>s is DisputeStatuses.Resolved or DisputeStatuses.Rejected;}
public sealed record DisputeHistoryItem(string? FromStatus,string ToStatus,string? Note,DateTime CreatedAtUtc);
public sealed record DisputeCaseItem(Guid Id,string CampaignTitle,string CampaignStatus,string BrandName,string KocName,string Category,string Subject,string Description,string Status,string Priority,string OpenedBy,string? ResolutionNote,DateTime CreatedAtUtc,IReadOnlyList<DisputeHistoryItem> History);
public sealed record DisputeCandidate(Guid ParticipationId,string CampaignTitle,string CounterpartyName);
public sealed record DisputePortal(IReadOnlyList<DisputeCandidate> Candidates,IReadOnlyList<DisputeCaseItem> Cases);
public sealed record DisputeMessageItem(Guid Id,string AuthorName,string AuthorRole,string Body,string? EvidenceUrl,string Visibility,DateTime CreatedAtUtc);
public sealed record DisputeCaseDetail(DisputeCaseItem Case,IReadOnlyList<DisputeMessageItem> Messages,bool CanReply,int OnHoldTransactionCount,decimal OnHoldAmount);
public sealed record DisputeResult(bool Success,string Message);
public interface IDisputeService
{
 Task<DisputePortal> GetPortalAsync(Guid userId,bool isBrand,CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null, Guid? campaignId = null);
 Task<IReadOnlyList<DisputeCaseItem>> GetAdminQueueAsync(string? status,CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null);
 Task<DisputeCaseDetail?> GetDetailAsync(Guid userId,string role,Guid caseId,CancellationToken ct=default);
 Task<DisputeResult> AddMessageAsync(Guid userId,string role,Guid caseId,string body,string? evidenceUrl,bool adminOnly,CancellationToken ct=default);
 Task<DisputeResult> OpenAsync(Guid userId,bool isBrand,Guid participationId,string category,string subject,string description,CancellationToken ct=default);
 Task<DisputeResult> CancelAsync(Guid userId,Guid caseId,string reason,CancellationToken ct=default);
 Task<DisputeResult> TakeAsync(Guid adminUserId,Guid caseId,CancellationToken ct=default);
 Task<DisputeResult> DecideAsync(Guid adminUserId,Guid caseId,string decision,string resolution,CancellationToken ct=default);
 Task<DisputeResult> CancelCampaignAsync(Guid adminUserId,Guid caseId,string reason,CancellationToken ct=default);
 Task<DisputeResult> ReleaseEarningsAsync(Guid adminUserId,Guid caseId,string reason,CancellationToken ct=default);
}
