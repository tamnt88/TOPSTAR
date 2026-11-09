using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
            .CountAsync(x => x.Status == "Published" || x.Status == "InProgress", cancellationToken);
        var totalBudgetRunning = await db.Campaigns.AsNoTracking()
            .Where(x => x.Status == "Published" || x.Status == "InProgress")
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
            return new BrandDashboardData("Brand", true, 0, 0, 0, 0m, [], []);
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

        var actionRows = await db.Campaigns.AsNoTracking()
            .Where(x => x.BrandProfileId == brand.Id && x.Status != "Cancelled" && x.Status != "Rejected")
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id, x.Title, x.Status, x.StartDate,
                FundingStatus = db.CampaignFundings.Where(f => f.CampaignId == x.Id).OrderByDescending(f => f.VersionNo).Select(f => f.Status).FirstOrDefault(),
                FundingDue = db.CampaignFundings.Where(f => f.CampaignId == x.Id).OrderByDescending(f => f.VersionNo).Select(f => f.DueAtUtc).FirstOrDefault(),
                ReviewCount = db.Submissions.Count(s => db.Participations.Any(p => p.Id == s.ParticipationId && p.CampaignId == x.Id) && s.Status == "Submitted"),
                ReviewDue = db.Submissions.Where(s => db.Participations.Any(p => p.Id == s.ParticipationId && p.CampaignId == x.Id) && s.Status == "Submitted").Join(db.CampaignDeliverables,s=>s.DeliverableId,d=>d.Id,(s,d)=>d.DueDateUtc).OrderBy(d=>d).FirstOrDefault(),
                ShipmentIssues = db.Shipments.Count(s => db.Participations.Any(p => p.Id == s.ParticipationId && p.CampaignId == x.Id) && (s.Status == "Issue" || s.Status == "Returned")),
                OpenDisputes = db.DisputeCases.Count(d => d.IsActive && db.Participations.Any(p => p.Id == d.ParticipationId && p.CampaignId == x.Id))
            }).Take(20).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var actions = actionRows.SelectMany(x =>
        {
            var list = new List<BrandActionItem>();
            if (x.Status == "NeedEdit") list.Add(new(x.Id,x.Title,"Cập nhật brief theo phản hồi","Brand","Khẩn cấp",null,$"/Brand/CampaignEdit?id={x.Id}"));
            else if (x.Status == "Submitted") list.Add(new(x.Id,x.Title,"Theo dõi duyệt chiến dịch","Topstar","Đang chờ",null,$"/Brand/CampaignWorkspace?id={x.Id}"));
            if (x.FundingStatus == "Pending") list.Add(new(x.Id,x.Title,"Hoàn tất hoặc theo dõi ký quỹ",x.FundingDue < now ? "Brand" : "Topstar",x.FundingDue < now ? "Quá hạn" : "Cần theo dõi",x.FundingDue,$"/Brand/CampaignWorkspace?id={x.Id}#tab-finance"));
            if (x.ReviewCount > 0) list.Add(new(x.Id,x.Title,$"Duyệt {x.ReviewCount} nội dung KOC","Brand",x.ReviewDue < now ? "Quá hạn" : "Cần làm",x.ReviewDue,$"/Brand/CampaignWorkspace?id={x.Id}#tab-execution-deliverables"));
            if (x.ShipmentIssues > 0) list.Add(new(x.Id,x.Title,$"Xử lý {x.ShipmentIssues} sự cố giao nhận","Brand","Khẩn cấp",null,$"/Brand/CampaignWorkspace?id={x.Id}#tab-execution-shipments"));
            if (x.OpenDisputes > 0) list.Add(new(x.Id,x.Title,$"Theo dõi {x.OpenDisputes} tranh chấp","Topstar","Khẩn cấp",null,$"/Brand/CampaignWorkspace?id={x.Id}#tab-support-disputes"));
            return list;
        }).OrderBy(x => x.Priority is "Khẩn cấp" or "Quá hạn" ? 0 : 1).ThenBy(x => x.DueAtUtc ?? DateTime.MaxValue).Take(8).ToList();

        return new BrandDashboardData(
            brand.CompanyName,
            IsIncompleteBrandProfile(user, brand),
            totalCampaigns,
            totalKocs,
            totalImpressions,
            totalBudget,
            recentCampaigns,
            actions);
    }

    public async Task<KocDashboardData> GetKocDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                User = x,
                Koc = db.KocProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var user = account?.User;
        var koc = account?.Koc;

        if (koc is null || user is null)
        {
            return new KocDashboardData("KOC", true, 0, 0, 0m, 0m, [], 0m, 0, 0, 0, 0, 0m, "Draft", [], []);
        }

        var body = await db.KocBodyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KocProfileId == koc.Id, cancellationToken);
        var hasAnySocial = await db.KocSocialAccounts.AsNoTracking()
            .AnyAsync(x => x.KocProfileId == koc.Id && x.ChannelUrl != null && x.ChannelUrl != "", cancellationToken);
        int categoryCount;
        try
        {
            categoryCount = await (
                from sc in db.KocSocialAccounts.AsNoTracking()
                join scc in db.KocSocialAccountCategories.AsNoTracking() on sc.Id equals scc.SocialAccountId
                where sc.KocProfileId == koc.Id
                select scc.CategoryId
            ).Distinct().CountAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Message.Contains("KocSocialAccountCategories", StringComparison.OrdinalIgnoreCase))
        {
            categoryCount = 0;
        }

        if (categoryCount == 0)
        {
            categoryCount = await db.KocProfileCategories.AsNoTracking()
                .CountAsync(x => x.KocProfileId == koc.Id, cancellationToken);
        }

        var participations = db.Participations.AsNoTracking().Where(x => x.KocProfileId == koc.Id);
        var pendingConfirmJobs = await db.Applications.AsNoTracking()
            .CountAsync(x => x.KocProfileId == koc.Id && (x.Status == "Applied" || x.Status == "Shortlisted"), cancellationToken);
        var activeJobs = await participations.CountAsync(x => x.Status == "Confirmed", cancellationToken);

        var estimatedIncome = await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                                     join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                                     join participation in db.Participations.AsNoTracking() on allocation.ParticipationId equals participation.Id
                                     where participation.KocProfileId == koc.Id &&
                                           (funding.Status == "Pending" || funding.Status == "Paid" || funding.Status == "PartiallyReleased")
                                     select (decimal?)(allocation.Amount - allocation.RefundedAmount))
            .SumAsync(cancellationToken) ?? 0m;

        var averageRating = await db.Reviews.AsNoTracking()
            .Where(x => x.RevieweeUserId == userId && x.Status == "Completed" && x.IsVisible)
            .AverageAsync(x => (decimal?)x.Rating, cancellationToken) ?? 0m;

        var pendingRows = await (from application in db.Applications.AsNoTracking()
                                 join campaign in db.Campaigns.AsNoTracking() on application.CampaignId equals campaign.Id
                                 join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                 where application.KocProfileId == koc.Id && (application.Status == "Applied" || application.Status == "Shortlisted")
                                 orderby application.UpdatedAtUtc ?? application.CreatedAtUtc descending
                                 select new { application.Id, campaign.Title, BrandName = brand.CompanyName, application.Status })
            .Take(5).ToListAsync(cancellationToken);
        var pendingJobs = pendingRows.Select(x => new KocJobItem(x.Title, x.BrandName, x.Status,
            $"/Koc/Jobs?section=applications&focusApplication={x.Id}#application-{x.Id}", "Xem đăng ký", null));
        var urgentTasks = await (from submission in db.Submissions.AsNoTracking()
                                    join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
                                    join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                                    join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                    join deliverable in db.CampaignDeliverables.AsNoTracking() on submission.DeliverableId equals deliverable.Id
                                    where participation.KocProfileId == koc.Id && submission.IsVisible &&
                                          (submission.Status == "NeedRevision" || submission.Status == "NotSubmitted")
                                    orderby submission.Status == "NeedRevision" ? 0 : 1,
                                            deliverable.DueDateUtc == null ? 1 : 0, deliverable.DueDateUtc
                                    select new { submission.Id, campaign.Title, BrandName = brand.CompanyName, submission.Status, deliverable.DueDateUtc })
            .Take(5).ToListAsync(cancellationToken);
        var taskJobs = urgentTasks.Select(x => new KocJobItem(x.Title, x.BrandName, x.Status,
            $"/Koc/Tasks?Status={x.Status}&focus={x.Id}", x.Status == "NeedRevision" ? "Sửa nội dung" : "Nộp nội dung", x.DueDateUtc));
        var todoJobs = taskJobs.Concat(pendingJobs).Take(5).ToList();
        var now = DateTime.UtcNow;
        var availableBalance = await db.WalletTransactions.AsNoTracking()
            .Where(x => x.KocProfileId == koc.Id && x.Status == "Available")
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var upcomingSchedulesQuery =
            from schedule in db.Schedules.AsNoTracking()
            join participation in db.Participations.AsNoTracking() on schedule.ParticipationId equals participation.Id
            join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
            join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
            where participation.KocProfileId == koc.Id
                  && schedule.StartsAtUtc >= now
                  && schedule.Status != "Cancelled"
            select new { Schedule = schedule, CampaignTitle = campaign.Title, BrandName = brand.CompanyName };
        var upcomingBookingCount = await upcomingSchedulesQuery.CountAsync(cancellationToken);
        var upcomingSchedules = await upcomingSchedulesQuery.OrderBy(x => x.Schedule.StartsAtUtc)
            .Take(4)
            .Select(x => new KocScheduleItem(x.Schedule.Id, x.CampaignTitle, x.BrandName, x.Schedule.Status, x.Schedule.StartsAtUtc!.Value))
            .ToListAsync(cancellationToken);
        var revisionRequiredCount = await (
            from submission in db.Submissions.AsNoTracking()
            join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
            where participation.KocProfileId == koc.Id && submission.Status == "NeedRevision"
            select submission.Id).CountAsync(cancellationToken);
        var unreadNotificationCount = await db.Notifications.AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.IsVisible && !x.IsRead && x.AudienceRole == "KOC", cancellationToken);
        var recentNotifications = await db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsVisible && x.AudienceRole == "KOC")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(4)
            .Select(x => new KocNotificationItem(x.Title, x.Body, x.IsRead, x.TargetUrl, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new KocDashboardData(
            string.IsNullOrWhiteSpace(koc.DisplayName) ? "KOC" : koc.DisplayName,
            IsIncompleteKocProfile(user, koc, body, hasAnySocial, categoryCount),
            pendingConfirmJobs,
            activeJobs,
            estimatedIncome,
            averageRating,
            todoJobs,
            availableBalance,
            upcomingBookingCount,
            revisionRequiredCount,
            unreadNotificationCount,
            koc.Followers,
            koc.Score,
            koc.Status,
            upcomingSchedules,
            recentNotifications);
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
           || !user.DateOfBirth.HasValue
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
