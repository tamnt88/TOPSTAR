using Topstar.Application.Campaigns;

namespace Topstar.Application.Reviews;

public static class ReviewRules
{
    public static bool IsValidRating(byte rating) => rating is >= 1 and <= 5;
    public static bool CanReviewCampaign(string campaignStatus) => campaignStatus == CampaignStatuses.Completed;
    public static bool CanModerate(string current, string target)
        => (current, target) is ("Completed", "Hidden") or ("Hidden", "Completed");
    public static bool IsValidReply(string? body) => body?.Trim().Length is >= 2 and <= 2000;
    public static bool IsValidAppealReason(string? reason) => reason?.Trim().Length is >= 10 and <= 2000;
    public static bool CanResolveAppeal(string current, string target)
        => current == "Open" && target is "Accepted" or "Rejected";
}

public interface ICampaignReviewService
{
    Task<IReadOnlyList<BrandReviewTask>> GetBrandTasksAsync(Guid brandUserId, Guid? campaignId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KocReviewTask>> GetKocTasksAsync(Guid kocUserId, CancellationToken cancellationToken = default);
    Task<KocReviewTaskPage> GetKocReviewTaskPageAsync(Guid kocUserId, KocReviewTaskQuery query, CancellationToken cancellationToken = default);
    Task<ReviewResult> ReviewKocAsync(Guid brandUserId, Guid participationId, byte rating, string? comment, CancellationToken cancellationToken = default);
    Task<ReviewResult> ReviewBrandAsync(Guid kocUserId, Guid participationId, byte rating, string? comment, CancellationToken cancellationToken = default);
    Task<ReviewResult> UpdateBrandReviewAsync(Guid kocUserId, Guid participationId, byte rating, string? comment, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminReviewItem>> GetAdminReviewsAsync(CancellationToken cancellationToken = default);
    Task<ReviewResult> ModerateAsync(Guid adminUserId, Guid reviewId, string targetStatus, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivedReviewItem>> GetReceivedReviewsAsync(Guid userId, Guid? campaignId = null, CancellationToken cancellationToken = default);
    Task<ReceivedReviewPage> GetReceivedReviewPageAsync(Guid userId, ReceivedReviewQuery query, CancellationToken cancellationToken = default);
    Task<ReviewResult> ReplyAsync(Guid userId, Guid reviewId, string body, CancellationToken cancellationToken = default);
    Task<ReviewResult> AppealAsync(Guid userId, Guid reviewId, string reason, CancellationToken cancellationToken = default);
    Task<ReviewResult> SupplementAppealAsync(Guid userId, Guid reviewId, string body, CancellationToken cancellationToken = default);
    Task<ReviewResult> WithdrawAppealAsync(Guid userId, Guid reviewId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminReviewAppealItem>> GetAdminAppealsAsync(CancellationToken cancellationToken = default);
    Task<ReviewResult> ResolveAppealAsync(Guid adminUserId, Guid appealId, string targetStatus, string decisionNote, CancellationToken cancellationToken = default);
}

public sealed record BrandReviewTask(Guid ParticipationId, Guid CampaignId, string CampaignTitle, string KocName, bool IsReviewed, byte? Rating, string? Comment, DateTime? ReviewedAtUtc);
public sealed record KocReviewTask(Guid ParticipationId, Guid CampaignId, string CampaignTitle, string BrandName, bool IsReviewed, byte? Rating, string? Comment, DateTime? ReviewedAtUtc);
public sealed record KocReviewTaskQuery(string? Search, DateTime? FromDate, DateTime? ToDate, byte? Rating, string? State, int Page, int PageSize);
public sealed record KocReviewTaskPage(IReadOnlyList<KocReviewTask> Items, int TotalCount, int Page, int PageSize, int PageCount, int PendingCount, int CompletedCount);
public sealed record ReviewResult(bool Success, string Message, Guid? Id = null);
public sealed record ReviewStatusEvent(string? FromStatus, string ToStatus, string? Reason, DateTime CreatedAtUtc);
public sealed record ReceivedReviewItem(Guid Id, string CampaignTitle, string ReviewerName, byte Rating, string? Comment, string Status, DateTime CreatedAtUtc, string? ReplyBody, DateTime? RepliedAtUtc, string? AppealStatus, string? AppealReason, string? AppealDecisionNote);
public sealed record ReceivedReviewQuery(string? Search, DateTime? FromDate, DateTime? ToDate, byte? Rating, string? State, int Page, int PageSize);
public sealed record ReceivedReviewPage(
    IReadOnlyList<ReceivedReviewItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int PageCount,
    int AllCount,
    int UnrepliedCount,
    double AverageRating);
public sealed record ReviewAppealEvent(string? FromStatus, string ToStatus, string? Note, DateTime CreatedAtUtc);
public sealed record AdminReviewAppealItem(Guid Id, Guid ReviewId, string CampaignTitle, string AppellantEmail, string ReviewerEmail, byte Rating, string? Comment, string Reason, string Status, string? DecisionNote, DateTime CreatedAtUtc)
{
    public IReadOnlyList<ReviewAppealEvent> History { get; init; } = [];
}
public sealed record AdminReviewItem(Guid Id, string CampaignTitle, string ReviewerEmail, string RevieweeEmail, byte Rating, string? Comment, string? ReplyBody, string Status, DateTime CreatedAtUtc)
{
    public IReadOnlyList<ReviewStatusEvent> History { get; init; } = [];
}
