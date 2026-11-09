namespace Topstar.Application.Reporting;
public interface IReportingService
{
 Task<BrandReport> GetBrandReportAsync(Guid brandUserId,CancellationToken ct=default,Guid? campaignId=null);
 Task<AdminOperationalReport> GetAdminReportAsync(CancellationToken ct=default);
}
public sealed record ReportCampaignOption(Guid Id,string Title);
public sealed record BrandKocPerformance(string KocName,long Impressions,long Views,long Engagements,decimal EngagementRate,int ApprovedSubmissions);
public sealed record BrandReport(int CampaignCount,int CompletedCampaigns,decimal TotalBudget,long Impressions,long Views,long Engagements,decimal EngagementRate,decimal SpendAmount,decimal RevenueAmount,decimal? Roi,IReadOnlyList<ReportCampaignOption> Campaigns,IReadOnlyList<BrandKocPerformance> KocPerformance);
public sealed record AdminOperationalReport(int CampaignCount,int CompletedCampaigns,decimal CompletionRate,int ActiveParticipations,int ShipmentIssueCount,int LateCancellationCount,int ActiveDisputeCount,decimal PaidPlatformFees,decimal PendingPlatformFees,int PendingPayoutCount,decimal PendingPayoutAmount);
