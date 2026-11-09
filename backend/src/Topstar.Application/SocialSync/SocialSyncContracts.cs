namespace Topstar.Application.SocialSync;

public enum SocialSyncScope
{
    KocSelf = 1,
    AdminSingleKoc = 2,
    AdminAllKoc = 3,
    BrandCampaign = 4
}

public sealed record SocialSyncRequestResult(
    bool Success,
    string Message,
    Guid? JobId = null,
    int EnqueuedJobs = 0);

public sealed record AdminSocialSyncKocItem(
    Guid KocProfileId,string DisplayName,string Email,string Platforms,string? LastStatus,DateTime? LastRequestedAtUtc);
public sealed record AdminSocialSyncJobItem(Guid Id,string KocName,string Scope,string RequestedByRole,string Status,string? ErrorMessage,DateTime RequestedAtUtc,DateTime? StartedAtUtc,DateTime? FinishedAtUtc);

public static class SocialSyncRules
{
    public static bool IsOpenJob(string? status)=>status is "Pending" or "Running";
}

public interface ISocialSyncOrchestrator
{
    Task<SocialSyncRequestResult> RequestKocSelfSyncAsync(Guid requesterUserId, CancellationToken cancellationToken = default);
    Task<SocialSyncRequestResult> RequestAdminSyncSingleKocAsync(Guid requesterUserId, Guid kocProfileId, CancellationToken cancellationToken = default);
    Task<SocialSyncRequestResult> RequestAdminSyncAllKocsAsync(Guid requesterUserId, CancellationToken cancellationToken = default);
    Task<SocialSyncRequestResult> RequestBrandCampaignSyncAsync(Guid requesterUserId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminSocialSyncKocItem>> GetAdminKocsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminSocialSyncJobItem>> GetAdminRecentJobsAsync(int take=30,CancellationToken cancellationToken = default);
    Task<int> ProcessPendingJobsAsync(int maxJobs, CancellationToken cancellationToken = default);
}

public interface ISocialTokenCipher
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}

public sealed record YouTubeProfileMetrics(
    int? Followers,
    int? AvgViews,
    int? VideosCount,
    string? ChannelHandle);

public interface IYouTubeMetricsClient
{
    Task<YouTubeProfileMetrics?> FetchProfileMetricsAsync(Guid connectionId, CancellationToken cancellationToken = default);
}

public sealed record TikTokProfileMetrics(
    int? Followers,
    int? AvgViews,
    int? VideosCount,
    string? DisplayName);

public interface ITikTokMetricsClient
{
    Task<TikTokProfileMetrics?> FetchProfileMetricsAsync(Guid connectionId, CancellationToken cancellationToken = default);
}
