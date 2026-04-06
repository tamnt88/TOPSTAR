using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Dashboard;

public sealed class DashboardDataService(TopstarDbContext db) : IDashboardDataService
{
    public async Task<AdminDashboardData> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var activeBrands = await db.BrandProfiles.AsNoTracking()
            .CountAsync(x => x.IsVisible && x.Status == "Approved", cancellationToken);
        var activeKocs = await db.KocProfiles.AsNoTracking()
            .CountAsync(x => x.IsVisible && x.Status == "Approved", cancellationToken);
        var runningCampaigns = await db.Campaigns.AsNoTracking()
            .CountAsync(x => x.Status == "Published" || x.Status == "Running", cancellationToken);
        var totalBudgetRunning = await db.Campaigns.AsNoTracking()
            .Where(x => x.Status == "Published" || x.Status == "Running")
            .SumAsync(x => (decimal?)x.BudgetAmount, cancellationToken) ?? 0m;
        var pendingCampaigns = await db.Campaigns.AsNoTracking()
            .CountAsync(x => x.Status == "Submitted" || x.Status == "NeedEdit", cancellationToken);
        var needReviewSubmissions = await db.Submissions.AsNoTracking()
            .CountAsync(x => x.Status == "Submitted", cancellationToken);
        var openShipmentIssues = await db.Shipments.AsNoTracking()
            .CountAsync(x => x.IssueNote != null && x.Status != "Resolved", cancellationToken);

        var campaignStatuses = await db.Campaigns.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new CampaignStatusItem(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentActivities = await db.AuthEvents.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(6)
            .Select(x => new ActivityItem(
                $"{x.EventType}: {(x.EmailOrPhone ?? "N/A")} ({x.EventStatus})",
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new AdminDashboardData(
            activeBrands,
            activeKocs,
            runningCampaigns,
            totalBudgetRunning,
            pendingCampaigns,
            needReviewSubmissions,
            openShipmentIssues,
            campaignStatuses,
            recentActivities);
    }

    public async Task<BrandDashboardData> GetBrandDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var brand = await db.BrandProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (brand is null)
        {
            return new BrandDashboardData("Brand", 0, 0, 0, 0m, []);
        }

        var campaigns = db.Campaigns.AsNoTracking().Where(x => x.BrandProfileId == brand.Id);
        var totalCampaigns = await campaigns.CountAsync(cancellationToken);
        var totalBudget = await campaigns.SumAsync(x => (decimal?)x.BudgetAmount, cancellationToken) ?? 0m;

        var campaignIds = await campaigns.Select(x => x.Id).ToListAsync(cancellationToken);
        var totalKocs = await db.Participations.AsNoTracking()
            .CountAsync(x => campaignIds.Contains(x.CampaignId), cancellationToken);
        var totalImpressions = await db.CampaignKocDailyMetrics.AsNoTracking()
            .Where(x => campaignIds.Contains(x.CampaignId))
            .SumAsync(x => (long?)x.Impressions, cancellationToken) ?? 0L;

        var recentCampaigns = await db.Campaigns.AsNoTracking()
            .Where(x => x.BrandProfileId == brand.Id)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Take(5)
            .Select(x => new BrandCampaignItem(
                x.Title,
                x.Status,
                db.Participations.Count(p => p.CampaignId == x.Id),
                db.CampaignKocDailyMetrics.Where(m => m.CampaignId == x.Id).Sum(m => (long?)m.Impressions) ?? 0L,
                x.BudgetAmount))
            .ToListAsync(cancellationToken);

        return new BrandDashboardData(
            brand.CompanyName,
            totalCampaigns,
            totalKocs,
            totalImpressions,
            totalBudget,
            recentCampaigns);
    }

    public async Task<KocDashboardData> GetKocDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var koc = await db.KocProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (koc is null)
        {
            return new KocDashboardData("KOC", 0, 0, 0m, 0m, []);
        }

        var participations = db.Participations.AsNoTracking().Where(x => x.KocProfileId == koc.Id);
        var pendingConfirmJobs = await participations.CountAsync(x => x.Status == "Shortlisted" || x.Status == "Applied", cancellationToken);
        var activeJobs = await participations.CountAsync(x => x.Status == "Confirmed" || x.Status == "Running", cancellationToken);

        var campaignIds = await participations.Select(x => x.CampaignId).Distinct().ToListAsync(cancellationToken);
        var estimatedIncome = await db.Campaigns.AsNoTracking()
            .Where(x => campaignIds.Contains(x.Id))
            .SumAsync(x => (decimal?)x.BudgetAmount, cancellationToken) ?? 0m;

        var averageRating = await db.Reviews.AsNoTracking()
            .Where(x => x.RevieweeUserId == userId && x.Status == "Approved")
            .AverageAsync(x => (decimal?)x.Rating, cancellationToken) ?? 0m;

        var todoJobs = await (
            from p in db.Participations.AsNoTracking()
            join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
            where p.KocProfileId == koc.Id && (p.Status == "Applied" || p.Status == "Shortlisted" || p.Status == "Confirmed")
            orderby p.UpdatedAtUtc ?? p.CreatedAtUtc descending
            select new KocJobItem(c.Title, c.BudgetAmount, p.Status)
        ).Take(5).ToListAsync(cancellationToken);

        return new KocDashboardData(
            string.IsNullOrWhiteSpace(koc.DisplayName) ? "KOC" : koc.DisplayName,
            pendingConfirmJobs,
            activeJobs,
            estimatedIncome,
            averageRating,
            todoJobs);
    }
}

