using Topstar.Application.Campaigns;

namespace Topstar.Application.Deliverables;

public static class SubmissionStatuses
{
    public const string NotSubmitted = "NotSubmitted";
    public const string Submitted = "Submitted";
    public const string NeedRevision = "NeedRevision";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class SubmissionTransitions
{
    public static bool CanSubmit(string status) => status is SubmissionStatuses.NotSubmitted or SubmissionStatuses.NeedRevision or SubmissionStatuses.Submitted;
    public static bool CanReview(string status) => status == SubmissionStatuses.Submitted;
    public static bool IsReviewDecision(string decision) => decision is SubmissionStatuses.Approved or SubmissionStatuses.NeedRevision or SubmissionStatuses.Rejected;
    public static bool IsResolved(string status) => status is SubmissionStatuses.Approved or SubmissionStatuses.Rejected;
}

public static class CampaignCompletionRules
{
    public static bool CanComplete(string campaignStatus, int submissionCount, int resolvedCount, bool bookingReady = true, bool shipmentReady = true, bool noActiveDisputes = true)
        => campaignStatus == CampaignStatuses.InProgress && submissionCount > 0 && resolvedCount == submissionCount && bookingReady && shipmentReady && noActiveDisputes;
}

public interface IDeliverableWorkflowService
{
    Task<IReadOnlyList<KocDeliverableTask>> GetKocTasksAsync(Guid kocUserId, CancellationToken cancellationToken = default);
    Task<KocDeliverableTaskPage> GetKocTaskPageAsync(Guid kocUserId, KocDeliverableTaskQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandSubmissionItem>> GetBrandSubmissionsAsync(Guid brandUserId, Guid? campaignId = null, CancellationToken cancellationToken = default);
    Task<BrandCampaignExecutionSummary?> GetBrandCampaignSummaryAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<DeliverableResult> SubmitAsync(Guid kocUserId, Guid submissionId, string? submissionUrl, string? note, string? attachmentUrl, string? attachmentName, CancellationToken cancellationToken = default);
    Task<SensitiveDataRedactionResult> RedactSensitiveDataAsync(Guid kocUserId, Guid historyId, bool redactLink, bool redactNote, bool redactAttachment, string? reason, CancellationToken cancellationToken = default);
    Task<DeliverableResult> ReviewAsync(Guid brandUserId, Guid submissionId, string decision, string? comment, CancellationToken cancellationToken = default);
    Task<DeliverableResult> CompleteCampaignAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default);
}

public sealed record KocDeliverableTaskQuery(string? Search, string? Status, string? Due, string? Sort, int Page, int PageSize);
public sealed record KocDeliverableTaskPage(
    IReadOnlyList<KocDeliverableTask> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int PageCount,
    int NotSubmittedCount,
    int NeedRevisionCount,
    int SubmittedCount,
    int ApprovedCount,
    int OverdueCount)
{
    public int UnfilteredCount { get; init; }
}
public sealed record SubmissionEventItem(Guid Id, string EventType, int RevisionNo, string Status, string? SubmissionUrl, string? ReviewComment, string? SubmissionNote, string? AttachmentUrl, string? AttachmentName, DateTime CreatedAtUtc);
public sealed record KocDeliverableTask(Guid SubmissionId, Guid CampaignId, string CampaignTitle, string BrandName, string DeliverableType, int Quantity, DateTime? DueDateUtc, string Status, string? SubmissionUrl, string? ReviewComment, int RevisionCount, DateTime? SubmittedAtUtc, bool CanSubmit)
{
    public IReadOnlyList<SubmissionEventItem> History { get; init; } = [];
    public string CampaignStatus { get; init; } = "";
    public bool FundingReady { get; init; }
    public string? CampaignObjective { get; init; }
    public decimal CampaignBudgetAmount { get; init; }
    public string CampaignPackageCode { get; init; } = "";
    public DateOnly? CampaignStartDate { get; init; }
    public DateOnly? CampaignEndDate { get; init; }
    public bool CampaignRequiresBooking { get; init; }
    public bool CampaignRequiresShipment { get; init; }
    public string? SubmissionNote { get; init; }
    public string? AttachmentUrl { get; init; }
    public string? AttachmentName { get; init; }
}
public sealed record BrandSubmissionItem(Guid SubmissionId, Guid CampaignId, string CampaignTitle, string KocName, string DeliverableType, int Quantity, DateTime? DueDateUtc, string Status, string? SubmissionUrl, string? ReviewComment, int RevisionCount, DateTime? SubmittedAtUtc, bool CanReview)
{
    public IReadOnlyList<SubmissionEventItem> History { get; init; } = [];
}
public sealed record BrandCampaignExecutionSummary(Guid CampaignId, string CampaignTitle, string CampaignStatus, int SubmissionCount, int ResolvedCount, int ApprovedCount, int RejectedCount, bool CanComplete, string CompletionHint);
public sealed record DeliverableResult(bool Success, string Message, Guid? Id = null);
public sealed record SensitiveDataRedactionResult(bool Success, string Message, string? StoredUrl = null);
