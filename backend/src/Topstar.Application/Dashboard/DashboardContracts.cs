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
    int TotalCampaigns,
    int TotalKocsInCampaigns,
    long TotalImpressions,
    decimal TotalBudget,
    IReadOnlyList<BrandCampaignItem> RecentCampaigns);

public sealed record KocDashboardData(
    string DisplayName,
    int PendingConfirmJobs,
    int ActiveJobs,
    decimal EstimatedIncome,
    decimal AverageRating,
    IReadOnlyList<KocJobItem> TodoJobs);

public sealed record CampaignStatusItem(string Label, int Count);
public sealed record ActivityItem(string Title, DateTime CreatedAtUtc);
public sealed record BrandCampaignItem(string Title, string Status, int KocCount, long Impressions, decimal Budget);
public sealed record KocJobItem(string CampaignTitle, decimal Budget, string Status);

