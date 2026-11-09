namespace Topstar.Application.Dashboard;

public interface IDashboardDataService
{
    Task<AdminDashboardData> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
    Task<BrandDashboardData> GetBrandDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<KocDashboardData> GetKocDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardData(
    int ActiveBrands,
    int ActiveKocs,
    int RunningCampaigns,
    decimal TotalBudgetRunningCampaigns,
    int PendingCampaigns,
    int NeedReviewSubmissions,
    int OpenShipmentIssues,
    IReadOnlyList<CampaignStatusItem> CampaignStatuses,
    IReadOnlyList<ActivityItem> RecentActivities);

public sealed record BrandDashboardData(
    string BrandName,
    bool NeedsProfileCompletion,
    int TotalCampaigns,
    int TotalKocsInCampaigns,
    long TotalImpressions,
    decimal TotalBudget,
    IReadOnlyList<BrandCampaignItem> RecentCampaigns,
    IReadOnlyList<BrandActionItem> ActionInbox);

public sealed record KocDashboardData(
    string DisplayName,
    bool NeedsProfileCompletion,
    int PendingConfirmJobs,
    int ActiveJobs,
    decimal EstimatedIncome,
    decimal AverageRating,
    IReadOnlyList<KocJobItem> TodoJobs,
    decimal AvailableBalance,
    int UpcomingBookingCount,
    int RevisionRequiredCount,
    int UnreadNotificationCount,
    int Followers,
    decimal ProfileScore,
    string ProfileStatus,
    IReadOnlyList<KocScheduleItem> UpcomingSchedules,
    IReadOnlyList<KocNotificationItem> RecentNotifications);

public sealed record CampaignStatusItem(string Label, int Count);
public sealed record ActivityItem(string Title, DateTime CreatedAtUtc);
public sealed record BrandCampaignItem(string Title, string Status, int KocCount, long Impressions, decimal Budget);
public sealed record BrandActionItem(Guid CampaignId,string CampaignTitle,string Title,string WaitingFor,string Priority,DateTime? DueAtUtc,string TargetUrl);
public sealed record KocJobItem(string CampaignTitle, string BrandName, string Status, string TargetUrl, string ActionLabel, DateTime? DueAtUtc);
public sealed record KocScheduleItem(Guid ScheduleId, string CampaignTitle, string BrandName, string Status, DateTime StartsAtUtc);
public sealed record KocNotificationItem(string Title, string Body, bool IsRead, string? TargetUrl, DateTime CreatedAtUtc);
