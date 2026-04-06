using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Infrastructure.Entities;
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

        var campaignStatusRows = await db.Campaigns.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var campaignStatuses = campaignStatusRows
            .Select(x => new CampaignStatusItem(x.Label, x.Count))
            .ToList();

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
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var brand = await db.BrandProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (brand is null || user is null)
        {
            return new BrandDashboardData("Brand", true, 0, 0, 0, 0m, []);
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
            IsIncompleteBrandProfile(user, brand),
            totalCampaigns,
            totalKocs,
            totalImpressions,
            totalBudget,
            recentCampaigns);
    }

    public async Task<KocDashboardData> GetKocDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var koc = await db.KocProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (koc is null || user is null)
        {
            return new KocDashboardData("KOC", true, 0, 0, 0m, 0m, []);
        }

        var body = await db.KocBodyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KocProfileId == koc.Id, cancellationToken);
        var hasAnySocial = await db.KocSocialAccounts.AsNoTracking()
            .AnyAsync(x => x.KocProfileId == koc.Id && x.ChannelUrl != null && x.ChannelUrl != "", cancellationToken);
        var categoryCount = await db.KocProfileCategories.AsNoTracking()
            .CountAsync(x => x.KocProfileId == koc.Id, cancellationToken);

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
            IsIncompleteKocProfile(user, koc, body, hasAnySocial, categoryCount),
            pendingConfirmJobs,
            activeJobs,
            estimatedIncome,
            averageRating,
            todoJobs);
    }

    private static bool IsIncompleteBrandProfile(UserEntity user, BrandProfileEntity brand)
        => string.Equals(brand.Status, "Draft", StringComparison.OrdinalIgnoreCase)
           || string.Equals(brand.Status, "NeedUpdate", StringComparison.OrdinalIgnoreCase)
           || string.IsNullOrWhiteSpace(user.Email)
           || string.IsNullOrWhiteSpace(user.Phone)
           || string.IsNullOrWhiteSpace(brand.CompanyName)
           || string.IsNullOrWhiteSpace(brand.ContactName)
           || string.IsNullOrWhiteSpace(brand.ContactPhone)
           || string.IsNullOrWhiteSpace(brand.StreetAddress)
           || !brand.ProvinceId.HasValue
           || !brand.WardId.HasValue
           || string.IsNullOrWhiteSpace(brand.TaxCode)
           || string.IsNullOrWhiteSpace(brand.TaxCompanyName)
           || string.IsNullOrWhiteSpace(brand.TaxInvoiceAddress)
           || string.IsNullOrWhiteSpace(brand.BusinessLicenseFileUrl);

    private static bool IsIncompleteKocProfile(
        UserEntity user,
        KocProfileEntity koc,
        KocBodyProfileEntity? body,
        bool hasAnySocial,
        int categoryCount)
        => string.Equals(koc.Status, "Draft", StringComparison.OrdinalIgnoreCase)
           || string.Equals(koc.Status, "NeedUpdate", StringComparison.OrdinalIgnoreCase)
           || string.IsNullOrWhiteSpace(user.Email)
           || string.IsNullOrWhiteSpace(user.Phone)
           || string.IsNullOrWhiteSpace(user.Gender)
           || string.IsNullOrWhiteSpace(koc.DisplayName)
           || string.IsNullOrWhiteSpace(koc.StreetAddress)
           || !koc.ProvinceId.HasValue
           || !koc.WardId.HasValue
           || body is null
           || !body.WeightKg.HasValue
           || !body.HeightCm.HasValue
           || string.IsNullOrWhiteSpace(body.MaritalStatus)
           || !hasAnySocial
           || categoryCount < 3;

}
