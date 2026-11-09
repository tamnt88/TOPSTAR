using Topstar.Core.Common.Pagination;
using System.Text.Json;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Application.Settings;

namespace Topstar.Infrastructure.Campaigns;

public sealed class CampaignService(TopstarDbContext db) : ICampaignService
{
    public async Task<CampaignResult> DeleteByAdminAsync(Guid campaignId, Guid adminUserId, string reason, CancellationToken cancellationToken = default)
    {
        reason = new HtmlSanitizer().Sanitize(reason?.Trim() ?? string.Empty);
        if (reason.Length is < 10 or > 1000) return new(false, "Lý do xóa phải từ 10 đến 1.000 ký tự.");
        var campaign = await db.Campaigns.AsNoTracking().Where(x => x.Id == campaignId)
            .Select(x => new { x.Id, x.Title, x.Status, x.BrandProfileId }).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @CampaignId uniqueidentifier = {campaignId};
DECLARE @ParticipationIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @SubmissionIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @DisputeIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @WalletIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @EscalationIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @ReviewIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @AppealIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @FundingIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @AllocationIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @EvidenceIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @InvoiceIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @ApplicationIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @ShortlistIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @PurchaseIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @ScheduleIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @ShipmentIds TABLE(Id uniqueidentifier PRIMARY KEY);
DECLARE @PostIds TABLE(Id uniqueidentifier PRIMARY KEY);

INSERT @ParticipationIds SELECT Id FROM Participations WHERE CampaignId=@CampaignId;
INSERT @SubmissionIds SELECT Id FROM Submissions WHERE ParticipationId IN(SELECT Id FROM @ParticipationIds) OR DeliverableId IN(SELECT Id FROM CampaignDeliverables WHERE CampaignId=@CampaignId);
INSERT @DisputeIds SELECT Id FROM DisputeCases WHERE ParticipationId IN(SELECT Id FROM @ParticipationIds);
INSERT @WalletIds SELECT Id FROM WalletTransactions WHERE CampaignId=@CampaignId OR ParticipationId IN(SELECT Id FROM @ParticipationIds) OR HoldDisputeCaseId IN(SELECT Id FROM @DisputeIds);
INSERT @EscalationIds SELECT Id FROM OperationalEscalations WHERE SubmissionId IN(SELECT Id FROM @SubmissionIds) OR DisputeCaseId IN(SELECT Id FROM @DisputeIds);
INSERT @ReviewIds SELECT Id FROM Reviews WHERE CampaignId=@CampaignId;
INSERT @AppealIds SELECT Id FROM ReviewAppeals WHERE ReviewId IN(SELECT Id FROM @ReviewIds);
INSERT @FundingIds SELECT Id FROM CampaignFundings WHERE CampaignId=@CampaignId;
INSERT @AllocationIds SELECT Id FROM CampaignFundingAllocations WHERE FundingId IN(SELECT Id FROM @FundingIds) OR ParticipationId IN(SELECT Id FROM @ParticipationIds);
INSERT @EvidenceIds SELECT Id FROM CampaignFundingEvidences WHERE FundingId IN(SELECT Id FROM @FundingIds);
INSERT @InvoiceIds SELECT Id FROM CampaignBillingInvoices WHERE CampaignId=@CampaignId;
INSERT @ApplicationIds SELECT Id FROM Applications WHERE CampaignId=@CampaignId;
INSERT @ShortlistIds SELECT Id FROM Shortlists WHERE CampaignId=@CampaignId;
INSERT @PurchaseIds SELECT Id FROM BrandPackagePurchases WHERE CampaignId=@CampaignId;
INSERT @ScheduleIds SELECT Id FROM Schedules WHERE ParticipationId IN(SELECT Id FROM @ParticipationIds);
INSERT @ShipmentIds SELECT Id FROM Shipments WHERE ParticipationId IN(SELECT Id FROM @ParticipationIds);
INSERT @PostIds SELECT Id FROM SubmissionPosts WHERE SubmissionId IN(SELECT Id FROM @SubmissionIds);

UPDATE Campaigns SET CancellationDisputeCaseId=NULL WHERE Id=@CampaignId;
DELETE FROM OperationalEscalationHistories WHERE EscalationId IN(SELECT Id FROM @EscalationIds);
DELETE FROM OperationalEscalations WHERE Id IN(SELECT Id FROM @EscalationIds);
DELETE FROM PostMetricSnapshots WHERE SubmissionPostId IN(SELECT Id FROM @PostIds);
DELETE FROM SubmissionPosts WHERE Id IN(SELECT Id FROM @PostIds);
DELETE FROM SubmissionHistories WHERE SubmissionId IN(SELECT Id FROM @SubmissionIds);
DELETE FROM ReviewAppealHistories WHERE AppealId IN(SELECT Id FROM @AppealIds);
DELETE FROM ReviewAppealSupplements WHERE AppealId IN(SELECT Id FROM @AppealIds);
DELETE FROM ReviewAppeals WHERE Id IN(SELECT Id FROM @AppealIds);
DELETE FROM ReviewReplies WHERE ReviewId IN(SELECT Id FROM @ReviewIds);
DELETE FROM ReviewRevisionHistories WHERE ReviewId IN(SELECT Id FROM @ReviewIds);
DELETE FROM ReviewStatusHistories WHERE ReviewId IN(SELECT Id FROM @ReviewIds);
DELETE FROM Reviews WHERE Id IN(SELECT Id FROM @ReviewIds);
DELETE FROM ScheduleReminderEvents WHERE ScheduleId IN(SELECT Id FROM @ScheduleIds);
DELETE FROM ScheduleStatusHistories WHERE ScheduleId IN(SELECT Id FROM @ScheduleIds);
DELETE FROM Schedules WHERE Id IN(SELECT Id FROM @ScheduleIds);
DELETE FROM ShipmentEvidence WHERE ShipmentId IN(SELECT Id FROM @ShipmentIds);
DELETE FROM ShipmentStatusHistories WHERE ShipmentId IN(SELECT Id FROM @ShipmentIds);
DELETE FROM ShipmentTrackingEvents WHERE ShipmentId IN(SELECT Id FROM @ShipmentIds);
DELETE FROM Shipments WHERE Id IN(SELECT Id FROM @ShipmentIds);
DELETE FROM WalletTransactionStatusHistories WHERE WalletTransactionId IN(SELECT Id FROM @WalletIds) OR DisputeCaseId IN(SELECT Id FROM @DisputeIds);
DELETE FROM WalletTransactions WHERE Id IN(SELECT Id FROM @WalletIds);
DELETE FROM DisputeCaseStatusHistories WHERE DisputeCaseId IN(SELECT Id FROM @DisputeIds);
DELETE FROM DisputeMessages WHERE DisputeCaseId IN(SELECT Id FROM @DisputeIds);
DELETE FROM DisputeCases WHERE Id IN(SELECT Id FROM @DisputeIds);
DELETE FROM CampaignFundLedgers WHERE FundingId IN(SELECT Id FROM @FundingIds) OR AllocationId IN(SELECT Id FROM @AllocationIds);
DELETE FROM CampaignFundingEvidenceReviews WHERE EvidenceId IN(SELECT Id FROM @EvidenceIds);
DELETE FROM CampaignFundingEvidences WHERE Id IN(SELECT Id FROM @EvidenceIds);
DELETE FROM CampaignFundingApprovals WHERE FundingId IN(SELECT Id FROM @FundingIds);
DELETE FROM CampaignFundingAllocations WHERE Id IN(SELECT Id FROM @AllocationIds);
DELETE FROM CampaignFundings WHERE Id IN(SELECT Id FROM @FundingIds);
DELETE FROM CampaignBillingInvoiceHistories WHERE InvoiceId IN(SELECT Id FROM @InvoiceIds);
DELETE FROM CampaignBillingInvoices WHERE Id IN(SELECT Id FROM @InvoiceIds);
DELETE FROM Submissions WHERE Id IN(SELECT Id FROM @SubmissionIds);
DELETE FROM ParticipationStatusHistories WHERE ParticipationId IN(SELECT Id FROM @ParticipationIds);
DELETE FROM Participations WHERE Id IN(SELECT Id FROM @ParticipationIds);
DELETE FROM ShortlistItems WHERE ApplicationId IN(SELECT Id FROM @ApplicationIds) OR ShortlistId IN(SELECT Id FROM @ShortlistIds);
DELETE FROM ApplicationStatusHistories WHERE ApplicationId IN(SELECT Id FROM @ApplicationIds);
DELETE FROM Applications WHERE Id IN(SELECT Id FROM @ApplicationIds);
DELETE FROM Shortlists WHERE Id IN(SELECT Id FROM @ShortlistIds);
DELETE FROM BrandPackagePurchaseHistories WHERE PurchaseId IN(SELECT Id FROM @PurchaseIds);
DELETE FROM BrandPackagePurchases WHERE Id IN(SELECT Id FROM @PurchaseIds);
DELETE FROM CampaignReviewFeedbackItems WHERE CampaignId=@CampaignId OR CampaignStatusHistoryId IN(SELECT Id FROM CampaignStatusHistories WHERE CampaignId=@CampaignId);
DELETE FROM CampaignStatusHistories WHERE CampaignId=@CampaignId;
DELETE FROM CampaignDeliverableHistories WHERE CampaignId=@CampaignId;
DELETE FROM CampaignDeliverables WHERE CampaignId=@CampaignId;
DELETE FROM CampaignKocDailyMetrics WHERE CampaignId=@CampaignId;
DELETE FROM CampaignTargetingHistories WHERE CampaignId=@CampaignId;
DELETE FROM SocialSyncJobs WHERE CampaignId=@CampaignId;
DELETE FROM CampaignTargetCategories WHERE CampaignId=@CampaignId;
DELETE FROM CampaignTargetProvinces WHERE CampaignId=@CampaignId;
DELETE FROM CampaignTargetings WHERE CampaignId=@CampaignId;
DELETE FROM KocCampaignFavorites WHERE CampaignId=@CampaignId;
DELETE FROM Campaigns WHERE Id=@CampaignId;", cancellationToken);

        db.AuditLogs.Add(new()
        {
            ActorUserId = adminUserId, EntityName = "Campaign", EntityId = campaignId.ToString(), ActionName = "Delete",
            PayloadJson = JsonSerializer.Serialize(new { campaign.Title, campaign.Status, campaign.BrandProfileId, Reason = reason, RelatedDataDeleted = true }), CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, $"Đã xóa chiến dịch “{campaign.Title}” và toàn bộ dữ liệu liên quan.");
    }
    public async Task<CampaignTargetingOptions> GetTargetingOptionsAsync(CancellationToken cancellationToken = default) => new(
        await db.Provinces.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new TargetingOption(x.Id,x.Name)).ToListAsync(cancellationToken),
        await db.KocCategories.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.SortOrder).ThenBy(x=>x.Name).Select(x=>new TargetingOption(x.Id,x.Name)).ToListAsync(cancellationToken));
    public async Task<IReadOnlyList<CampaignListItem>> GetBrandCampaignsAsync(Guid brandUserId, CancellationToken cancellationToken = default)
    {
        var brandId = await db.BrandProfiles.AsNoTracking().Where(x => x.UserId == brandUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!brandId.HasValue) return [];
        var items = await QueryList().Where(x => x.BrandProfileId == brandId.Value).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CampaignListItem(x.Id, x.Title, x.BrandName, x.BudgetAmount, x.PackageCode, x.Status, x.StartDate, x.EndDate, x.ApplicationCount, null) { IsShortlistFinalized = x.IsShortlistFinalized })
            .ToListAsync(cancellationToken);
        var ids = items.Select(x => x.Id).ToArray();
        var histories = await db.CampaignStatusHistories.AsNoTracking().Where(x => ids.Contains(x.CampaignId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(x => x with { History = histories.Where(h => h.CampaignId == x.Id).Select(h => new LifecycleEvent(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList() }).ToList();
    }

    public async Task<BrandCampaignPage> GetBrandCampaignPageAsync(Guid brandUserId, BrandCampaignPageRequest request, CancellationToken cancellationToken = default)
    {
        var brandId = await db.BrandProfiles.AsNoTracking().Where(x => x.UserId == brandUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!brandId.HasValue) return new([], 0, 0, new(0, 0, 0, 0));

        var owned = QueryList().Where(x => x.BrandProfileId == brandId.Value);
        var totalCount = await owned.CountAsync(cancellationToken);
        var summary = await owned.GroupBy(_ => 1).Select(group => new BrandCampaignSummary(
            group.Count(x => x.Status == CampaignStatuses.Draft || x.Status == CampaignStatuses.NeedEdit),
            group.Count(x => x.Status == CampaignStatuses.Submitted),
            group.Count(x => x.Status == CampaignStatuses.Published || x.Status == CampaignStatuses.InProgress),
            group.Count(x => x.Status == CampaignStatuses.Completed))).FirstOrDefaultAsync(cancellationToken) ?? new(0, 0, 0, 0);

        var filtered = owned;
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword)) filtered = filtered.Where(x => x.Title.Contains(keyword) || x.PackageCode.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(request.Status)) filtered = filtered.Where(x => x.Status == request.Status);
        var filteredCount = await filtered.CountAsync(cancellationToken);

        filtered = (request.SortColumn, request.SortDescending) switch
        {
            (1, false) => filtered.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAtUtc),
            (1, true) => filtered.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAtUtc),
            (2, false) => filtered.OrderBy(x => x.BudgetAmount).ThenByDescending(x => x.CreatedAtUtc),
            (2, true) => filtered.OrderByDescending(x => x.BudgetAmount).ThenByDescending(x => x.CreatedAtUtc),
            (3, false) => filtered.OrderBy(x => x.StartDate).ThenByDescending(x => x.CreatedAtUtc),
            (3, true) => filtered.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.CreatedAtUtc),
            (4, false) => filtered.OrderBy(x => x.SelectedKocCount).ThenByDescending(x => x.CreatedAtUtc),
            (4, true) => filtered.OrderByDescending(x => x.SelectedKocCount).ThenByDescending(x => x.CreatedAtUtc),
            (5, false) => filtered.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAtUtc),
            (5, true) => filtered.OrderByDescending(x => x.Status).ThenByDescending(x => x.CreatedAtUtc),
            _ => filtered.OrderByDescending(x => x.CreatedAtUtc)
        };

        var start = Math.Max(0, request.Start);
        var length = PaginationDefaults.Normalize(request.Length);
        var items = await filtered.Skip(start).Take(length)
            .Select(x => new CampaignListItem(x.Id, x.Title, x.BrandName, x.BudgetAmount, x.PackageCode, x.Status, x.StartDate, x.EndDate, x.ApplicationCount, null) { IsShortlistFinalized = x.IsShortlistFinalized, AvatarUrl=x.AvatarUrl, BannerUrl=x.BannerUrl, DesiredKocCount=x.DesiredKocCount, SelectedKocCount=x.SelectedKocCount, FundedBudget=x.FundedBudget, HasWorkStarted=x.HasWorkStarted })
            .ToListAsync(cancellationToken);
        return new(items, totalCount, filteredCount, summary);
    }

    public async Task<IReadOnlyList<CampaignListItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
        => await QueryList().Where(x => x.Status == CampaignStatuses.Submitted).OrderBy(x => x.CreatedAtUtc)
            .Select(x => new CampaignListItem(x.Id, x.Title, x.BrandName, x.BudgetAmount, x.PackageCode, x.Status, x.StartDate, x.EndDate, x.ApplicationCount, null) { IsShortlistFinalized = x.IsShortlistFinalized })
            .ToListAsync(cancellationToken);

    public async Task<AdminCampaignPage> GetAdminCampaignPageAsync(BrandCampaignPageRequest request, CancellationToken cancellationToken = default)
    {
        var source = QueryList();
        var totalCount = await source.CountAsync(cancellationToken);
        var summary = await source.GroupBy(_ => 1).Select(group => new AdminCampaignSummary(
            group.Count(),
            group.Count(x => x.Status == CampaignStatuses.Draft),
            group.Count(x => x.Status == CampaignStatuses.Submitted),
            group.Count(x => x.Status == CampaignStatuses.Published || x.Status == CampaignStatuses.InProgress),
            group.Count(x => x.Status == CampaignStatuses.Completed),
            group.Count(x => x.Status == CampaignStatuses.NeedEdit || x.Status == CampaignStatuses.Rejected || x.Status == CampaignStatuses.Cancelled)))
            .FirstOrDefaultAsync(cancellationToken) ?? new(0, 0, 0, 0, 0, 0);

        var filtered = source;
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            filtered = filtered.Where(x => x.Title.Contains(keyword) || x.BrandName.Contains(keyword) || x.PackageCode.Contains(keyword));
        if (request.Status == "Active") filtered = filtered.Where(x => x.Status == CampaignStatuses.Published || x.Status == CampaignStatuses.InProgress);
        else if (request.Status == "Attention") filtered = filtered.Where(x => x.Status == CampaignStatuses.NeedEdit || x.Status == CampaignStatuses.Rejected || x.Status == CampaignStatuses.Cancelled);
        else if (!string.IsNullOrWhiteSpace(request.Status)) filtered = filtered.Where(x => x.Status == request.Status);
        var filteredCount = await filtered.CountAsync(cancellationToken);

        filtered = (request.SortColumn, request.SortDescending) switch
        {
            (1, false) => filtered.OrderBy(x => x.Title),
            (1, true) => filtered.OrderByDescending(x => x.Title),
            (2, false) => filtered.OrderBy(x => x.BrandName),
            (2, true) => filtered.OrderByDescending(x => x.BrandName),
            (3, false) => filtered.OrderBy(x => x.BudgetAmount),
            (3, true) => filtered.OrderByDescending(x => x.BudgetAmount),
            (4, false) => filtered.OrderBy(x => x.StartDate),
            (4, true) => filtered.OrderByDescending(x => x.StartDate),
            (5, false) => filtered.OrderBy(x => x.ApplicationCount),
            (5, true) => filtered.OrderByDescending(x => x.ApplicationCount),
            (6, false) => filtered.OrderBy(x => x.Status),
            (6, true) => filtered.OrderByDescending(x => x.Status),
            _ => filtered.OrderByDescending(x => x.CreatedAtUtc)
        };
        var start = Math.Max(0, request.Start);
        var length = PaginationDefaults.Normalize(request.Length);
        var items = await filtered.Skip(start).Take(length)
            .Select(x => new CampaignListItem(x.Id, x.Title, x.BrandName, x.BudgetAmount, x.PackageCode, x.Status, x.StartDate, x.EndDate, x.ApplicationCount, null)
            { IsShortlistFinalized = x.IsShortlistFinalized })
            .ToListAsync(cancellationToken);
        return new(items, totalCount, filteredCount, summary);
    }

    public async Task<CampaignDiscoveryPage> GetPublishedAsync(Guid? kocUserId, CampaignDiscoveryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new(null, null, null, null, null, null, "newest");
        var pageSize = filter.PageSize is 12 or 24 or 48 ? filter.PageSize : 12;
        var requestedPage = Math.Max(1, filter.Page);
        var kocId = kocUserId.HasValue
            ? await db.KocProfiles.AsNoTracking().Where(x => x.UserId == kocUserId.Value).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken)
            : null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = QueryList().Where(x =>
            x.Status == CampaignStatuses.Published &&
            x.IsVisible &&
            x.StartDate.HasValue &&
            x.StartDate.Value > today &&
            (!x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value));
        if(!string.IsNullOrWhiteSpace(filter?.Keyword)){var keyword=filter.Keyword.Trim();query=query.Where(x=>x.Title.Contains(keyword)||x.BrandName.Contains(keyword));}
        if(filter?.MinimumBudget is >0)query=query.Where(x=>x.BudgetAmount>=filter.MinimumBudget.Value);
        if(filter?.MaximumBudget is >0)query=query.Where(x=>x.BudgetAmount<=filter.MaximumBudget.Value);
        if(filter?.RequiresBooking is not null)query=query.Where(x=>x.RequiresBooking==filter.RequiresBooking.Value);
        if(filter?.RequiresShipment is not null)query=query.Where(x=>x.RequiresShipment==filter.RequiresShipment.Value);
        if(!string.IsNullOrWhiteSpace(filter?.DeliverableType)){var type=filter.DeliverableType.Trim();query=query.Where(x=>db.CampaignDeliverables.Any(d=>d.CampaignId==x.Id&&d.DeliverableType.Contains(type)));}
        query=filter?.Sort switch{"budget_desc"=>query.OrderByDescending(x=>x.BudgetAmount).ThenByDescending(x=>x.CreatedAtUtc),"budget_asc"=>query.OrderBy(x=>x.BudgetAmount).ThenByDescending(x=>x.CreatedAtUtc),"ending"=>query.OrderBy(x=>x.EndDate).ThenByDescending(x=>x.CreatedAtUtc),_=>query.OrderByDescending(x=>x.CreatedAtUtc)};
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var page = Math.Min(requestedPage, totalPages);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CampaignListItem(x.Id, x.Title, x.BrandName, x.BudgetAmount, x.PackageCode, x.Status, x.StartDate, x.EndDate, x.ApplicationCount,
                kocId.HasValue ? db.Applications.Where(a => a.CampaignId == x.Id && a.KocProfileId == kocId.Value).Select(a => a.Status).FirstOrDefault() : null)
                {
                    RequiresBooking = x.RequiresBooking,
                    AvatarUrl = x.AvatarUrl,
                    BannerUrl = x.BannerUrl,
                    RequiresShipment = x.RequiresShipment,
                    DeliverableType = x.DeliverableType,
                    DesiredKocCount = x.DesiredKocCount,
                    DeliverableQuantity = db.CampaignDeliverables.Where(d => d.CampaignId == x.Id).Sum(d => (int?)d.Quantity) ?? 0,
                    DeliverableDueDateUtc = db.CampaignDeliverables.Where(d => d.CampaignId == x.Id && d.DueDateUtc.HasValue).Min(d => d.DueDateUtc)
                })
            .ToListAsync(cancellationToken);
        if (kocId.HasValue && items.Count > 0)
        {
            items = await AttachDiscoveryMatchesAsync(items, kocId.Value, cancellationToken);
            var favoriteIds = await db.KocCampaignFavorites.AsNoTracking().Where(x => x.KocProfileId == kocId.Value && items.Select(i => i.Id).Contains(x.CampaignId)).Select(x => x.CampaignId).ToListAsync(cancellationToken);
            items = items.Select(item => item with { IsFavorite = favoriteIds.Contains(item.Id) }).ToList();
        }
        return new(items, totalCount, page, pageSize);
    }

    public async Task<CampaignResult> ToggleFavoriteAsync(Guid campaignId, Guid kocUserId, CancellationToken cancellationToken = default)
    {
        var kocId = await db.KocProfiles.Where(x => x.UserId == kocUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!kocId.HasValue) return new(false, "Không tìm thấy hồ sơ KOC.");
        var campaignExists = await db.Campaigns.AnyAsync(x => x.Id == campaignId && x.Status == CampaignStatuses.Published && x.IsVisible, cancellationToken);
        if (!campaignExists) return new(false, "Chiến dịch không còn khả dụng.");
        var favorite = await db.KocCampaignFavorites.FirstOrDefaultAsync(x => x.KocProfileId == kocId.Value && x.CampaignId == campaignId, cancellationToken);
        if (favorite is null) db.KocCampaignFavorites.Add(new() { Id = Guid.NewGuid(), KocProfileId = kocId.Value, CampaignId = campaignId, CreatedAtUtc = DateTime.UtcNow });
        else db.KocCampaignFavorites.Remove(favorite);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, favorite is null ? "Đã lưu việc để xem sau." : "Đã bỏ lưu việc.");
    }

    private async Task<List<CampaignListItem>> AttachDiscoveryMatchesAsync(List<CampaignListItem> items, Guid kocId, CancellationToken cancellationToken)
    {
        var profile = await db.KocProfiles.AsNoTracking()
            .Where(x => x.Id == kocId)
            .Select(x => new { x.Gender, x.ProvinceId, x.Followers })
            .FirstAsync(cancellationToken);
        var engagement = await db.KocSocialAccounts.AsNoTracking()
            .Where(x => x.KocProfileId == kocId && x.IsVisible)
            .MaxAsync(x => (decimal?)x.AvgEngagementRate, cancellationToken);
        var kocCategoryIds = await db.KocProfileCategories.AsNoTracking()
            .Where(x => x.KocProfileId == kocId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        var campaignIds = items.Select(x => x.Id).ToArray();
        var targets = await db.CampaignTargetings.AsNoTracking()
            .Where(x => campaignIds.Contains(x.CampaignId))
            .ToListAsync(cancellationToken);
        var provinceLinks = await db.CampaignTargetProvinces.AsNoTracking()
            .Where(x => campaignIds.Contains(x.CampaignId))
            .ToListAsync(cancellationToken);
        var categoryLinks = await db.CampaignTargetCategories.AsNoTracking()
            .Where(x => campaignIds.Contains(x.CampaignId))
            .ToListAsync(cancellationToken);
        var provinceMap = provinceLinks.GroupBy(x => x.CampaignId).ToDictionary(x => x.Key, x => x.Select(y => y.ProvinceId).ToHashSet());
        var categoryMap = categoryLinks.GroupBy(x => x.CampaignId).ToDictionary(x => x.Key, x => x.Select(y => y.CategoryId).ToHashSet());
        var matchMap = new Dictionary<Guid, CampaignTargetingMatch>();
        foreach (var target in targets)
        {
            var misses = new List<string>();
            if (target.Gender != "Any" && !string.Equals(target.Gender, profile.Gender, StringComparison.OrdinalIgnoreCase)) misses.Add("Giới tính");
            if (target.MinimumFollowers.HasValue && profile.Followers < target.MinimumFollowers) misses.Add("Follower tối thiểu");
            if (target.MaximumFollowers.HasValue && profile.Followers > target.MaximumFollowers) misses.Add("Follower tối đa");
            if (target.MinimumEngagementRate.HasValue && engagement.GetValueOrDefault() < target.MinimumEngagementRate) misses.Add("Engagement tối thiểu");
            if (target.MaximumEngagementRate.HasValue && engagement.GetValueOrDefault() > target.MaximumEngagementRate) misses.Add("Engagement tối đa");
            if (provinceMap.TryGetValue(target.CampaignId, out var provinceIds) && provinceIds.Count > 0 && (!profile.ProvinceId.HasValue || !provinceIds.Contains(profile.ProvinceId.Value))) misses.Add("Tỉnh/thành");
            if (categoryMap.TryGetValue(target.CampaignId, out var targetCategoryIds) && targetCategoryIds.Count > 0 && !targetCategoryIds.Overlaps(kocCategoryIds)) misses.Add("Nhóm nội dung");
            matchMap[target.CampaignId] = new(misses.Count == 0, misses);
        }
        return items.Select(item => item with { CurrentKocTargetingMatch = matchMap.GetValueOrDefault(item.Id) }).ToList();
    }

    public async Task<CampaignDetail?> GetForBrandAsync(Guid campaignId, Guid brandUserId, CancellationToken cancellationToken = default)
    {
        var ownsCampaign = await (from c in db.Campaigns.AsNoTracking()
                                  join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                                  where c.Id == campaignId && b.UserId == brandUserId
                                  select c.Id).AnyAsync(cancellationToken);
        if(!ownsCampaign)return null;return await AttachDeliverablesAsync(await GetDetailQuery(campaignId).FirstOrDefaultAsync(cancellationToken),cancellationToken);
    }

    public async Task<CampaignDetail?> GetForAdminAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var detail = await AttachDeliverablesAsync(await GetDetailQuery(campaignId).FirstOrDefaultAsync(cancellationToken), cancellationToken);
        if (detail is null) return null;
        var history = await db.CampaignStatusHistories.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new LifecycleEvent(x.FromStatus, x.ToStatus, x.Note, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return detail with { History = history };
    }

    public async Task<CampaignDetail?> GetPublishedDetailAsync(Guid campaignId, Guid? kocUserId, CancellationToken cancellationToken = default)
    {
        var isPublic = await db.Campaigns.AsNoTracking()
            .AnyAsync(x => x.Id == campaignId && x.Status == CampaignStatuses.Published && x.IsVisible, cancellationToken);
        if (!isPublic)
        {
            // Recruitment may close, but the applicant must retain access to their brief.
            var isApplicant = kocUserId.HasValue && await (
                from application in db.Applications.AsNoTracking()
                join profile in db.KocProfiles.AsNoTracking() on application.KocProfileId equals profile.Id
                where application.CampaignId == campaignId && profile.UserId == kocUserId.Value
                select application.Id).AnyAsync(cancellationToken);
            if (!isApplicant) return null;
        }
        var detail = await GetDetailQuery(campaignId).FirstOrDefaultAsync(cancellationToken);
        if (detail is null) return null;
        detail = (await AttachDeliverablesAsync(detail, cancellationToken))!;
        if (!kocUserId.HasValue) return detail;
        var kocId = await db.KocProfiles.AsNoTracking().Where(x => x.UserId == kocUserId.Value).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        var status = kocId.HasValue ? await db.Applications.AsNoTracking().Where(x => x.CampaignId == campaignId && x.KocProfileId == kocId.Value).Select(x => x.Status).FirstOrDefaultAsync(cancellationToken) : null;
        CampaignTargetingMatch? match=null;
        if(kocId.HasValue&&detail.Targeting is not null)
        {
            var profile=await db.KocProfiles.AsNoTracking().Where(x=>x.Id==kocId.Value).Select(x=>new{x.Gender,x.ProvinceId,x.Followers}).FirstAsync(cancellationToken);
            var engagement=await db.KocSocialAccounts.AsNoTracking().Where(x=>x.KocProfileId==kocId.Value&&x.IsVisible).MaxAsync(x=>(decimal?)x.AvgEngagementRate,cancellationToken);
            var categoryIds=await db.KocProfileCategories.AsNoTracking().Where(x=>x.KocProfileId==kocId.Value).Select(x=>x.CategoryId).ToListAsync(cancellationToken);
            var misses=new List<string>();var target=detail.Targeting;
            if(target.Gender!="Any"&&!string.Equals(target.Gender,profile.Gender,StringComparison.OrdinalIgnoreCase))misses.Add("Giới tính");
            if(target.MinimumFollowers.HasValue&&profile.Followers<target.MinimumFollowers)misses.Add("Follower tối thiểu");if(target.MaximumFollowers.HasValue&&profile.Followers>target.MaximumFollowers)misses.Add("Follower tối đa");
            if(target.MinimumEngagementRate.HasValue&&engagement.GetValueOrDefault()<target.MinimumEngagementRate)misses.Add("Engagement tối thiểu");if(target.MaximumEngagementRate.HasValue&&engagement.GetValueOrDefault()>target.MaximumEngagementRate)misses.Add("Engagement tối đa");
            if(target.Provinces.Count>0&&(!profile.ProvinceId.HasValue||!target.Provinces.Any(x=>x.Id==profile.ProvinceId.Value)))misses.Add("Tỉnh/thành");if(target.Categories.Count>0&&!target.Categories.Any(x=>categoryIds.Contains(x.Id)))misses.Add("Nhóm nội dung");
            match=new(misses.Count==0,misses);
        }
        var isFavorite = kocId.HasValue && await db.KocCampaignFavorites.AsNoTracking().AnyAsync(x => x.KocProfileId == kocId.Value && x.CampaignId == campaignId, cancellationToken);
        return detail with { CurrentApplicationStatus = status,CurrentKocTargetingMatch=match,IsFavorite=isFavorite };
    }

    public async Task<CampaignResult> CreateDraftAsync(Guid brandUserId, CampaignUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var deliverableValidation=await ValidateDeliverableTypesAsync(request,null,cancellationToken);if(deliverableValidation is not null)return deliverableValidation;
        if (!request.PackagePurchaseId.HasValue && !request.CheckoutAtCreation) return new(false, "Vui lòng xác định nhu cầu chiến dịch trước.");
        var targetingValidation = await ValidateTargetingAsync(request.Targeting, cancellationToken); if (targetingValidation is not null) return targetingValidation;
        var brandId = await db.BrandProfiles.Where(x => x.UserId == brandUserId && x.Status == "Approved").Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!brandId.HasValue) return new(false, "Hồ sơ Brand chưa được duyệt.");
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var now=DateTime.UtcNow;
        BrandPackagePurchaseEntity? purchase=null;
        Finance.CampaignCreationPolicy? creationPolicy=null;
        if(request.CheckoutAtCreation){
            creationPolicy=await Finance.CampaignCreationRules.ReadAsync(db,cancellationToken);
            if(creationPolicy is null)return new(false,"Cần triển khai SQL 169 trước khi tạo chiến dịch theo luồng mới.");
            var needsError=Finance.CampaignCreationRules.Validate(creationPolicy,request.BudgetAmount,request.DesiredKocCount,request.BrandSelectionRequested);
            if(needsError is not null)return new(false,needsError);
            request=request with{PackageCode=request.BrandSelectionRequested?"CUSTOM_SELF":"CUSTOM_ASSIGNED"};
        }else{
        purchase = await db.BrandPackagePurchases.FirstOrDefaultAsync(x => x.Id == request.PackagePurchaseId!.Value
            && x.BrandProfileId == brandId.Value, cancellationToken);

        if (purchase is null || purchase.Status != BrandPackagePurchaseStatuses.Paid || purchase.CampaignId.HasValue
            || (purchase.ExpiresAtUtc.HasValue && purchase.ExpiresAtUtc <= now))
            return new(false, "Gói mua không hợp lệ, chưa được thanh toán, đã hết hạn hoặc đã dùng cho chiến dịch khác.");
        var purchaseQuota = purchase.SelectionQuotaSnapshot;
        if (!purchaseQuota.HasValue)
            purchaseQuota = await db.CampaignPackagePolicies.AsNoTracking()
                .Where(x => x.Id == purchase.PackagePolicyId && !x.IsDeleted)
                .Select(x => x.SelectionQuota).FirstOrDefaultAsync(cancellationToken);
        if (!purchaseQuota.HasValue)
            return new(false, $"Gói {purchase.PackageNameSnapshot} chưa có giới hạn KOC. Vui lòng liên hệ Topstar để cập nhật quyền lợi gói.");
        if (request.DesiredKocCount > purchaseQuota.Value)
            return new(false, $"Gói {purchase.PackageNameSnapshot} cho phép xác nhận tối đa {purchaseQuota.Value} KOC.");
        request=request with{PackageCode=purchase.PackageCodeSnapshot};
        }
        var campaign = new CampaignEntity { Id = Guid.NewGuid(), BrandProfileId = brandId.Value, Status = CampaignStatuses.Draft, IsVisible = false, CreatedAtUtc = now };
        Apply(campaign, request, now);
        if(creationPolicy is not null){campaign.CheckoutAtCreation=true;campaign.BrandSelectionSnapshot=request.BrandSelectionRequested;campaign.SelectionQuotaSnapshot=request.DesiredKocCount;campaign.KocPlatformFeeRateSnapshot=creationPolicy.KocPlatformFeeRate;}
        campaign.AvatarUrl = request.AvatarUrl;
        campaign.BannerUrl = request.BannerUrl;
        db.Campaigns.Add(campaign);
        AddCampaignHistory(campaign.Id, null, CampaignStatuses.Draft, "Campaign created", brandUserId, now);
        foreach (var d in NormalizeDeliverables(request))
        {
            var entity = new CampaignDeliverableEntity { Id = Guid.NewGuid(), CampaignId = campaign.Id, DeliverableType = d.DeliverableType, Quantity = d.Quantity, DueDateUtc = d.DueDateUtc, CreatedAtUtc = now };
            db.CampaignDeliverables.Add(entity);
            AddDeliverableHistory(campaign.Id, entity.Id, "Created", null, d, brandUserId, now);
        }
        await ApplyTargetingAsync(campaign.Id, brandUserId, request.Targeting, now, cancellationToken);
        AddAudit(brandUserId, campaign.Id, "CreateDraft", new { request.Title, request.Execution });
        if(purchase is not null){
        purchase.CampaignId = campaign.Id;
        purchase.Status = BrandPackagePurchaseStatuses.Consumed;
        purchase.ConsumedAtUtc = now;
        purchase.UpdatedAtUtc = now;
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=purchase.Id,FromStatus=BrandPackagePurchaseStatuses.Paid,ToStatus=BrandPackagePurchaseStatuses.Consumed,Note=$"Đã dùng để tạo chiến dịch {request.Title}.",ActorUserId=brandUserId,CreatedAtUtc=now});
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, "Đã lưu thông tin chiến dịch. Vui lòng kiểm tra và thanh toán.", campaign.Id);
    }

    public async Task<CampaignResult> UpdateImagesAsync(Guid campaignId, Guid brandUserId, string? avatarUrl, string? bannerUrl, CancellationToken cancellationToken = default)
    {
        if(avatarUrl is null && bannerUrl is null)return new(false,"Vui lòng chọn ảnh đại diện hoặc banner.");
        bool Valid(string? path)=>path is null || path.StartsWith("/uploads/",StringComparison.Ordinal) && path.Length<=500 && !path.Contains("..") && !path.Contains('\\');
        if(!Valid(avatarUrl)||!Valid(bannerUrl))return new(false,"Đường dẫn hình ảnh không hợp lệ.");
        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,cancellationToken);
        var campaign=await OwnedCampaign(campaignId,brandUserId,cancellationToken);
        if(campaign is null)return new(false,"Không tìm thấy chiến dịch hoặc bạn không có quyền sửa.");
        var started=await db.Participations.AnyAsync(x=>x.CampaignId==campaignId&&x.WorkOpenedAtUtc!=null,cancellationToken);
        if(!CampaignTransitions.CanEditImages(campaign.Status,started))return new(false,"Chỉ được đổi hình ảnh khi chiến dịch chưa bắt đầu triển khai.");
        var before=new{campaign.AvatarUrl,campaign.BannerUrl};
        if(avatarUrl is not null)campaign.AvatarUrl=avatarUrl;
        if(bannerUrl is not null)campaign.BannerUrl=bannerUrl;
        campaign.UpdatedAtUtc=DateTime.UtcNow;
        db.AuditLogs.Add(new(){ActorUserId=brandUserId,EntityName="Campaign",EntityId=campaignId.ToString(),ActionName="UpdateImages",PayloadJson=JsonSerializer.Serialize(new{Before=before,After=new{campaign.AvatarUrl,campaign.BannerUrl}}),CreatedAtUtc=DateTime.UtcNow});
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true,"Đã cập nhật hình ảnh chiến dịch.",campaignId);
    }

    public async Task<CampaignResult> UpdateDraftAsync(Guid campaignId, Guid brandUserId, CampaignUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var deliverableValidation=await ValidateDeliverableTypesAsync(request,campaignId,cancellationToken);if(deliverableValidation is not null)return deliverableValidation;
        var targetingValidation = await ValidateTargetingAsync(request.Targeting, cancellationToken); if (targetingValidation is not null) return targetingValidation;
        var campaign = await OwnedCampaign(campaignId, brandUserId, cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch hoặc bạn không có quyền sửa.");
        if (!CampaignTransitions.CanEdit(campaign.Status)) return new(false, "Chỉ được sửa chiến dịch Nháp hoặc Cần chỉnh sửa.");
        if(campaign.CheckoutAtCreation){
            if(request.BudgetAmount!=campaign.BudgetAmount||request.DesiredKocCount!=campaign.DesiredKocCount)
                return new(false,"Ngân sách và số KOC đã xác nhận ở bước nhu cầu; không được thay đổi trong nội dung chiến dịch.");
        }else{
        var purchasedPolicy = await db.BrandPackagePurchases.AsNoTracking().Where(x => x.CampaignId == campaign.Id)
            .Select(x => new { SelectionQuota=x.SelectionQuotaSnapshot, x.PackageNameSnapshot, x.PackagePolicyId })
            .FirstOrDefaultAsync(cancellationToken);
        var selectionQuota = purchasedPolicy?.SelectionQuota;
        var packageName = purchasedPolicy?.PackageNameSnapshot;
        if (purchasedPolicy is null || !selectionQuota.HasValue)
        {
            var legacyPolicy = await db.CampaignPackagePolicies.AsNoTracking()
                .Where(x => !x.IsDeleted && (purchasedPolicy != null ? x.Id == purchasedPolicy.PackagePolicyId : x.Code == campaign.PackageCode))
                .Select(x => new { x.SelectionQuota, x.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (legacyPolicy is null) return new(false, "Không tìm thấy chính sách gói của chiến dịch cũ. Vui lòng liên hệ Topstar.");
            selectionQuota = legacyPolicy.SelectionQuota; packageName ??= legacyPolicy.Name;
        }
        if (!selectionQuota.HasValue) return new(false, $"Gói {packageName} chưa có giới hạn KOC. Vui lòng liên hệ Topstar để cập nhật quyền lợi gói.");
        if (selectionQuota is int quota && request.DesiredKocCount > quota)
            return new(false, $"Gói {packageName} cho phép xác nhận tối đa {quota} KOC.");
        }
        request = request with { PackageCode = campaign.PackageCode };
        var now = DateTime.UtcNow; Apply(campaign, request, now); campaign.ReviewNote = null;
        if (request.AvatarUrl is not null) campaign.AvatarUrl = request.AvatarUrl;
        if (request.BannerUrl is not null) campaign.BannerUrl = request.BannerUrl;
        var existingDeliverables = await db.CampaignDeliverables.Where(x => x.CampaignId == campaign.Id).OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var requested = NormalizeDeliverables(request);
        var removedDeliverables = existingDeliverables.Skip(requested.Count).ToList();
        if (removedDeliverables.Count > 0)
        {
            var removedIds = removedDeliverables.Select(x => x.Id).ToList();
            if (await db.Submissions.AnyAsync(x => removedIds.Contains(x.DeliverableId), cancellationToken))
                return new(false, "Không thể xóa deliverable đã phát sinh lịch sử bài nộp. Hãy giữ deliverable này để bảo toàn hồ sơ.", campaign.Id);
        }
        for (var i = 0; i < requested.Count; i++)
        {
            var deliverable = requested[i];
            if (i < existingDeliverables.Count)
            {
                var before = new CampaignDeliverableInput(existingDeliverables[i].DeliverableType, existingDeliverables[i].Quantity, existingDeliverables[i].DueDateUtc);
                existingDeliverables[i].DeliverableType = deliverable.DeliverableType;
                existingDeliverables[i].Quantity = deliverable.Quantity;
                existingDeliverables[i].DueDateUtc = deliverable.DueDateUtc;
                existingDeliverables[i].UpdatedAtUtc = now;
                if (before != deliverable) AddDeliverableHistory(campaign.Id, existingDeliverables[i].Id, "Updated", before, deliverable, brandUserId, now);
            }
            else
            {
                var entity = new CampaignDeliverableEntity { Id = Guid.NewGuid(), CampaignId = campaign.Id, DeliverableType = deliverable.DeliverableType, Quantity = deliverable.Quantity, DueDateUtc = deliverable.DueDateUtc, CreatedAtUtc = now };
                db.CampaignDeliverables.Add(entity);
                AddDeliverableHistory(campaign.Id, entity.Id, "Created", null, deliverable, brandUserId, now);
            }
        }
        foreach (var removed in removedDeliverables)
        {
            AddDeliverableHistory(campaign.Id, removed.Id, "Deleted", new(removed.DeliverableType, removed.Quantity, removed.DueDateUtc), null, brandUserId, now);
            db.CampaignDeliverables.Remove(removed);
        }
        await ApplyTargetingAsync(campaign.Id, brandUserId, request.Targeting, now, cancellationToken);
        AddAudit(brandUserId, campaign.Id, "UpdateDraft", new { request.Title, request.Execution });
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã cập nhật chiến dịch.", campaign.Id);
    }

    public async Task<CampaignResult> SubmitAsync(Guid campaignId, Guid brandUserId, CancellationToken cancellationToken = default)
    {
        var campaign = await OwnedCampaign(campaignId, brandUserId, cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch hoặc bạn không có quyền submit.");
        if (!CampaignTransitions.CanSubmit(campaign.Status)) return new(false, "Trạng thái hiện tại không cho phép submit.");
        if(campaign.CheckoutAtCreation && !await db.CampaignFundings.AnyAsync(f=>f.CampaignId==campaignId&&(f.Status=="Paid"||f.Status=="PartiallyReleased"||f.Status=="Released"),cancellationToken))return new(false,"Vui lòng hoàn tất thanh toán trước khi gửi duyệt nội dung.");
        var before = campaign.Status; var now = DateTime.UtcNow;
        campaign.Status = CampaignStatuses.Submitted; campaign.IsVisible = false; campaign.ReviewNote = null; campaign.UpdatedAtUtc = now;
        AddCampaignHistory(campaign.Id, before, CampaignStatuses.Submitted, null, brandUserId, now);
        AddAudit(brandUserId, campaign.Id, "Submit", null); await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã gửi chiến dịch để Admin duyệt.", campaign.Id);
    }

    public async Task<CampaignResult> ReviewAsync(Guid campaignId, Guid adminUserId, CampaignReviewRequest request, CancellationToken cancellationToken = default)
    {
        var reviewFeedback = request.FeedbackItems.Where(x => !string.IsNullOrWhiteSpace(x.FieldCode) && !string.IsNullOrWhiteSpace(x.Message)).ToList();
        var reviewFields = new[] { "Title", "Brief", "Budget", "Schedule", "Deliverables", "Targeting", "Logistics", "Other" };
        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch.");
        if (!CampaignTransitions.CanReview(campaign.Status)) return new(false, "Chỉ được duyệt chiến dịch đang ở trạng thái Submitted.");
        var decision = request.Decision.Trim();
        if (decision == CampaignStatuses.NeedEdit && reviewFeedback.Count == 0) return new(false, "Cần chọn ít nhất một hạng mục và ghi rõ nội dung Brand phải sửa.");
        if (reviewFeedback.Count > 8 || reviewFeedback.Any(x => !reviewFields.Contains(x.FieldCode) || x.Message.Trim().Length > 500) || reviewFeedback.Select(x => x.FieldCode).Distinct().Count() != reviewFeedback.Count) return new(false, "Checklist chỉnh sửa không hợp lệ.");
        if (decision is not (CampaignStatuses.Published or CampaignStatuses.NeedEdit or CampaignStatuses.Rejected)) return new(false, "Quyết định duyệt không hợp lệ.");
        if (decision != CampaignStatuses.Published && string.IsNullOrWhiteSpace(request.ReviewNote)) return new(false, "Vui lòng nhập lý do khi yêu cầu sửa hoặc từ chối.");
        var before = campaign.Status; var now = DateTime.UtcNow;
        campaign.Status = decision; campaign.IsVisible = decision == CampaignStatuses.Published; campaign.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim(); campaign.UpdatedAtUtc = now;
        var reviewHistoryId = AddCampaignHistory(campaign.Id, before, decision, campaign.ReviewNote, adminUserId, now);
        if (decision == CampaignStatuses.NeedEdit)
            for (var i = 0; i < reviewFeedback.Count; i++) db.CampaignReviewFeedbackItems.Add(new() { Id = Guid.NewGuid(), CampaignId = campaign.Id, CampaignStatusHistoryId = reviewHistoryId, FieldCode = reviewFeedback[i].FieldCode, Message = reviewFeedback[i].Message.Trim(), SortOrder = i, ActorUserId = adminUserId, CreatedAtUtc = now });
        AddAudit(adminUserId, campaign.Id, decision, new { campaign.ReviewNote }); await db.SaveChangesAsync(cancellationToken);
        return new(true, decision == CampaignStatuses.Published ? "Đã duyệt và công khai chiến dịch." : "Đã cập nhật kết quả duyệt.", campaign.Id);
    }

    private IQueryable<CampaignRow> QueryList() =>
        from c in db.Campaigns.AsNoTracking()
        join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
        select new CampaignRow
        {
            Id = c.Id,
            BrandProfileId = c.BrandProfileId,
            BrandUserId = b.UserId,
            Title = c.Title,
            AvatarUrl = c.AvatarUrl,
            BannerUrl = c.BannerUrl,
            BrandName = b.CompanyName,
            BudgetAmount = c.BudgetAmount,
            PackageCode = c.PackageCode,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DesiredKocCount = c.DesiredKocCount,
            CreatedAtUtc = c.CreatedAtUtc,
            IsVisible = c.IsVisible,
            SelectedKocCount = db.Applications.Count(a=>a.CampaignId==c.Id&&a.Status==ApplicationStatuses.Confirmed),
            HasWorkStarted = db.Participations.Any(p=>p.CampaignId==c.Id&&p.WorkOpenedAtUtc!=null),
            FundedBudget = db.CampaignFundings.Where(f=>f.CampaignId==c.Id&&(f.Status=="Paid"||f.Status=="PartiallyReleased"||f.Status=="Released"||f.Status=="Refunded"))
                .Select(f=>(decimal?)(f.CampaignBudgetSnapshot-f.RefundedAmount)).Sum()??0,
            ApplicationCount = db.Applications.Count(a => a.CampaignId == c.Id &&
                (a.Status == ApplicationStatuses.Applied || a.Status == ApplicationStatuses.Shortlisted || a.Status == ApplicationStatuses.Confirmed)),
            IsShortlistFinalized = db.Shortlists.Any(s => s.CampaignId == c.Id && s.FinalizedAtUtc != null),
            RequiresBooking = c.RequiresBooking,
            RequiresShipment = c.RequiresShipment,
            DeliverableType = db.CampaignDeliverables
                .Where(d => d.CampaignId == c.Id)
                .OrderBy(d => d.CreatedAtUtc)
                .Select(d => d.DeliverableType)
                .FirstOrDefault() ?? ""
        };

    private IQueryable<CampaignDetail> GetDetailQuery(Guid campaignId) =>
        from c in db.Campaigns.AsNoTracking()
        join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
        where c.Id == campaignId
        let d = db.CampaignDeliverables.AsNoTracking().FirstOrDefault(x => x.CampaignId == c.Id)
        select new CampaignDetail(c.Id, c.Title, b.CompanyName, c.Objective, c.BudgetAmount, c.PackageCode, c.RequiresBooking, c.RequiresShipment, c.Status, c.StartDate, c.EndDate, c.ReviewNote, d != null ? d.DeliverableType : "", d != null ? d.Quantity : 0, d != null ? d.DueDateUtc : null, db.Applications.Count(a => a.CampaignId == c.Id && (a.Status == ApplicationStatuses.Applied || a.Status == ApplicationStatuses.Shortlisted || a.Status == ApplicationStatuses.Confirmed)), null) { AvatarUrl = c.AvatarUrl, BannerUrl = c.BannerUrl, AllocationMode = c.AllocationMode, DesiredKocCount = c.DesiredKocCount };

    private async Task<CampaignDetail?> AttachDeliverablesAsync(CampaignDetail? detail,CancellationToken ct){if(detail is null)return null;var rows=await db.CampaignDeliverables.AsNoTracking().Where(x=>x.CampaignId==detail.Id).OrderBy(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).Select(x=>new CampaignDeliverableItem(x.Id,x.DeliverableType,x.Quantity,x.DueDateUtc)).ToListAsync(ct);var reviewId=await db.CampaignStatusHistories.AsNoTracking().Where(x=>x.CampaignId==detail.Id&&x.ToStatus==CampaignStatuses.NeedEdit).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(ct);var feedback=reviewId.HasValue?await db.CampaignReviewFeedbackItems.AsNoTracking().Where(x=>x.CampaignStatusHistoryId==reviewId.Value).OrderBy(x=>x.SortOrder).Select(x=>new CampaignReviewFeedbackItem(x.FieldCode,x.Message,x.SortOrder)).ToListAsync(ct):[];var target=await db.CampaignTargetings.AsNoTracking().FirstOrDefaultAsync(x=>x.CampaignId==detail.Id,ct);CampaignTargetingItem? targeting=null;if(target is not null){var provinces=await(from link in db.CampaignTargetProvinces.AsNoTracking() join p in db.Provinces.AsNoTracking() on link.ProvinceId equals p.Id where link.CampaignId==detail.Id orderby p.Name select new TargetingOption(p.Id,p.Name)).ToListAsync(ct);var categories=await(from link in db.CampaignTargetCategories.AsNoTracking() join c in db.KocCategories.AsNoTracking() on link.CategoryId equals c.Id where link.CampaignId==detail.Id orderby c.Name select new TargetingOption(c.Id,c.Name)).ToListAsync(ct);targeting=new(target.Gender,target.MinimumFollowers,target.MaximumFollowers,target.MinimumEngagementRate,target.MaximumEngagementRate,provinces,categories);}var executionRow=await db.Campaigns.AsNoTracking().Where(x=>x.Id==detail.Id).Select(x=>new{x.ExecutionMode,x.VenueName,x.VenueAddress,x.ContactName,x.ContactPhone,x.ExecutionSchedule,x.BookingInstructions,x.ShippingInstructions,x.SupportDetails,x.ExecutionRequirements}).FirstAsync(ct);var execution=new CampaignExecutionItem(executionRow.ExecutionMode,executionRow.VenueName,executionRow.VenueAddress,executionRow.ContactName,executionRow.ContactPhone,executionRow.ExecutionSchedule,executionRow.BookingInstructions,executionRow.ShippingInstructions,executionRow.SupportDetails,executionRow.ExecutionRequirements);return detail with{Deliverables=rows,ReviewFeedbackItems=feedback,Targeting=targeting,Execution=execution};}
    private async Task<CampaignResult?> ValidateTargetingAsync(CampaignTargetingInput target, CancellationToken ct)
    {
        var ruleError=CampaignTargetingRules.Validate(target);if(ruleError is not null)return new(false,ruleError switch{"Gender"=>"Giới tính targeting không hợp lệ.","Followers"=>"Khoảng follower không hợp lệ.","Engagement"=>"Khoảng engagement phải nằm trong 0–100%.",_=>"Mỗi nhóm targeting được chọn tối đa 20 giá trị."});
        var provinceIds=target.ProvinceIds.Distinct().ToArray();var categoryIds=target.CategoryIds.Distinct().ToArray();
        if(await db.Provinces.CountAsync(x=>provinceIds.Contains(x.Id)&&x.IsActive,ct)!=provinceIds.Length)return new(false,"Tỉnh/thành targeting không tồn tại hoặc đã ngừng sử dụng.");
        if(await db.KocCategories.CountAsync(x=>categoryIds.Contains(x.Id),ct)!=categoryIds.Length)return new(false,"Nhóm nội dung targeting không tồn tại.");
        return null;
    }
    private async Task ApplyTargetingAsync(Guid campaignId,Guid actor,CampaignTargetingInput input,DateTime now,CancellationToken ct)
    {
        var provinceIds=input.ProvinceIds.Distinct().Order().ToArray();var categoryIds=input.CategoryIds.Distinct().Order().ToArray();
        var entity=await db.CampaignTargetings.FirstOrDefaultAsync(x=>x.CampaignId==campaignId,ct);
        var oldProvinces=entity is null?[]:await db.CampaignTargetProvinces.Where(x=>x.CampaignId==campaignId).ToListAsync(ct);
        var oldCategories=entity is null?[]:await db.CampaignTargetCategories.Where(x=>x.CampaignId==campaignId).ToListAsync(ct);
        string Snapshot(string gender,int? minF,int? maxF,decimal? minE,decimal? maxE,IEnumerable<Guid> provinces,IEnumerable<Guid> categories)=>JsonSerializer.Serialize(new{Gender=gender,MinimumFollowers=minF,MaximumFollowers=maxF,MinimumEngagementRate=minE,MaximumEngagementRate=maxE,ProvinceIds=provinces.Order(),CategoryIds=categories.Order()});
        var before=entity is null?null:Snapshot(entity.Gender,entity.MinimumFollowers,entity.MaximumFollowers,entity.MinimumEngagementRate,entity.MaximumEngagementRate,oldProvinces.Select(x=>x.ProvinceId),oldCategories.Select(x=>x.CategoryId));
        var after=Snapshot(input.Gender,input.MinimumFollowers,input.MaximumFollowers,input.MinimumEngagementRate,input.MaximumEngagementRate,provinceIds,categoryIds);
        if(before==after)return;
        if(entity is null){entity=new(){CampaignId=campaignId,CreatedAtUtc=now};db.CampaignTargetings.Add(entity);}else entity.UpdatedAtUtc=now;
        entity.Gender=input.Gender;entity.MinimumFollowers=input.MinimumFollowers;entity.MaximumFollowers=input.MaximumFollowers;entity.MinimumEngagementRate=input.MinimumEngagementRate;entity.MaximumEngagementRate=input.MaximumEngagementRate;
        var removedProvinces=oldProvinces.Where(x=>!provinceIds.Contains(x.ProvinceId)).ToList();var removedCategories=oldCategories.Where(x=>!categoryIds.Contains(x.CategoryId)).ToList();
        if(removedProvinces.Count>0)db.CampaignTargetProvinces.RemoveRange(removedProvinces);if(removedCategories.Count>0)db.CampaignTargetCategories.RemoveRange(removedCategories);
        var oldProvinceIds=oldProvinces.Select(x=>x.ProvinceId).ToHashSet();var oldCategoryIds=oldCategories.Select(x=>x.CategoryId).ToHashSet();
        db.CampaignTargetProvinces.AddRange(provinceIds.Where(x=>!oldProvinceIds.Contains(x)).Select(x=>new CampaignTargetProvinceEntity{CampaignId=campaignId,ProvinceId=x,CreatedAtUtc=now}));
        db.CampaignTargetCategories.AddRange(categoryIds.Where(x=>!oldCategoryIds.Contains(x)).Select(x=>new CampaignTargetCategoryEntity{CampaignId=campaignId,CategoryId=x,CreatedAtUtc=now}));
        db.CampaignTargetingHistories.Add(new(){Id=Guid.NewGuid(),CampaignId=campaignId,Action=before is null?"Created":"Updated",FromSnapshotJson=before,ToSnapshotJson=after,ActorUserId=actor,CreatedAtUtc=now});
    }
    private static List<CampaignDeliverableInput> NormalizeDeliverables(CampaignUpsertRequest r)=>(r.Deliverables.Count>0?r.Deliverables:[new(r.DeliverableType,r.DeliverableQuantity,r.DeliverableDueDateUtc)]).Select(x=>new CampaignDeliverableInput(x.DeliverableType.Trim(),x.Quantity,x.DueDateUtc)).ToList();
    private async Task<CampaignResult?> ValidateDeliverableTypesAsync(CampaignUpsertRequest request,Guid? campaignId,CancellationToken ct)
    {
        var codes=NormalizeDeliverables(request).Select(x=>x.DeliverableType).Distinct().ToArray();
        var allowed=await db.OperationalCatalogItems.AsNoTracking().Where(x=>x.CatalogType==OperationalCatalogTypes.Deliverable&&x.IsActive&&codes.Contains(x.Code)).Select(x=>x.Code).ToListAsync(ct);
        if(campaignId.HasValue)allowed.AddRange(await db.CampaignDeliverables.AsNoTracking().Where(x=>x.CampaignId==campaignId&&codes.Contains(x.DeliverableType)).Select(x=>x.DeliverableType).ToListAsync(ct));
        var allowedCodes = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return codes.All(allowedCodes.Contains)?null:new(false,"Loại nội dung không có trong cấu hình hoặc đang tạm ẩn.");
    }
    private async Task<CampaignEntity?> OwnedCampaign(Guid id, Guid userId, CancellationToken ct) => await (from c in db.Campaigns join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id == id && b.UserId == userId select c).FirstOrDefaultAsync(ct);
    private Task<string?> ResolvePackageCodeAsync(decimal budget,CancellationToken ct)=>db.CampaignPackagePolicies.AsNoTracking().Where(x=>x.IsActive&&!x.IsDeleted&&x.MinBudgetAmount<=budget&&(!x.MaxBudgetAmount.HasValue||x.MaxBudgetAmount>=budget)).OrderByDescending(x=>x.MinBudgetAmount).Select(x=>x.Code).FirstOrDefaultAsync(ct);
    private static CampaignResult? Validate(CampaignUpsertRequest r) { if (string.IsNullOrWhiteSpace(r.Title)) return new(false, "Tên chiến dịch là bắt buộc."); if (r.BudgetAmount <= 0) return new(false, "Ngân sách phải lớn hơn 0."); if (!r.StartDate.HasValue || !r.EndDate.HasValue) return new(false, "Vui lòng chọn đầy đủ ngày bắt đầu và ngày kết thúc."); if(r.CheckoutAtCreation&&r.StartDate<CampaignScheduleRules.TodayVietnam)return new(false,"Ngày bắt đầu dự kiến phải từ hôm nay trở đi."); if (r.EndDate < r.StartDate) return new(false, "Ngày kết thúc phải sau ngày bắt đầu.");var ds=r.Deliverables.Count>0?r.Deliverables:[new(r.DeliverableType,r.DeliverableQuantity,r.DeliverableDueDateUtc)];if(ds.Count is <1 or >10||ds.Any(x=>string.IsNullOrWhiteSpace(x.DeliverableType)||x.DeliverableType.Trim().Length>100||x.Quantity is <1 or >100))return new(false,"Cần 1–10 nội dung bàn giao; loại tối đa 100 ký tự và số lượng 1–100.");foreach(var d in ds){if(r.CheckoutAtCreation&&d.DueDateUtc.HasValue&&DateOnly.FromDateTime(d.DueDateUtc.Value)<CampaignScheduleRules.TodayVietnam)return new(false,"Hạn nộp phải từ hôm nay trở đi.");if(r.CheckoutAtCreation&&!d.DueDateUtc.HasValue)return new(false,"Vui lòng chọn ngày và giờ hạn nộp cho từng nội dung bàn giao.");var deadlineError=CampaignScheduleRules.ValidateDeadline(r.StartDate,r.EndDate,d.DueDateUtc);if(deadlineError is not null)return new(false,deadlineError);}var e=r.Execution;if(!CampaignExecutionModes.IsValid(e.Mode))return new(false,"Hình thức thực hiện không hợp lệ.");if(!r.CheckoutAtCreation&&CampaignExecutionModes.IsOnsite(e.Mode)&&(string.IsNullOrWhiteSpace(e.VenueName)||string.IsNullOrWhiteSpace(e.VenueAddress)||string.IsNullOrWhiteSpace(e.ContactName)||string.IsNullOrWhiteSpace(e.ContactPhone)||string.IsNullOrWhiteSpace(e.Schedule)))return new(false,"Chiến dịch tại địa điểm phải có đủ địa điểm, người liên hệ, số điện thoại và lịch thực hiện.");if(!r.CheckoutAtCreation&&e.Mode==CampaignExecutionModes.LivestreamOnline&&string.IsNullOrWhiteSpace(e.Schedule))return new(false,"Vui lòng nhập lịch livestream dự kiến.");if((e.VenueName?.Length??0)>200||(e.VenueAddress?.Length??0)>500||(e.ContactName?.Length??0)>100||(e.ContactPhone?.Length??0)>20||(e.Schedule?.Length??0)>500||(e.BookingInstructions?.Length??0)>1000||(e.ShippingInstructions?.Length??0)>1000||(e.SupportDetails?.Length??0)>1000||(e.Requirements?.Length??0)>2000)return new(false,"Thông tin yêu cầu thực hiện vượt quá độ dài cho phép.");return null; }
    private static void Apply(CampaignEntity c, CampaignUpsertRequest r, DateTime now) { c.DesiredKocCount = Math.Clamp(r.DesiredKocCount, 1, 100); var e=r.Execution;string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();c.Title = r.Title.Trim(); c.Objective = SanitizeCampaignContent(r.Objective); c.BudgetAmount = r.BudgetAmount; c.AllocationMode = r.AllocationMode is "Custom" ? "Custom" : "Equal"; c.PackageCode = string.IsNullOrWhiteSpace(r.PackageCode) ? "STANDARD" : r.PackageCode.Trim().ToUpperInvariant(); c.ExecutionMode=e.Mode;c.RequiresBooking=CampaignExecutionModes.IsOnsite(e.Mode);c.RequiresShipment=e.Mode==CampaignExecutionModes.RemoteProductReview;c.VenueName=CampaignExecutionModes.IsOnsite(e.Mode)?Clean(e.VenueName):null;c.VenueAddress=CampaignExecutionModes.IsOnsite(e.Mode)?Clean(e.VenueAddress):null;c.ContactName=CampaignExecutionModes.IsOnsite(e.Mode)?Clean(e.ContactName):null;c.ContactPhone=CampaignExecutionModes.IsOnsite(e.Mode)?Clean(e.ContactPhone):null;c.ExecutionSchedule=Clean(e.Schedule);c.BookingInstructions=CampaignExecutionModes.IsOnsite(e.Mode)?Clean(e.BookingInstructions):null;c.ShippingInstructions=e.Mode==CampaignExecutionModes.RemoteProductReview?Clean(e.ShippingInstructions):null;c.SupportDetails=Clean(e.SupportDetails);c.ExecutionRequirements=Clean(e.Requirements); c.StartDate = r.StartDate; c.EndDate = r.EndDate; c.UpdatedAtUtc = now; }
    private static string? SanitizeCampaignContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.UnionWith(["h2", "h3", "h4", "p", "br", "ul", "ol", "li", "strong", "em", "u", "s", "blockquote", "pre", "code", "a", "figure", "figcaption", "img", "table", "thead", "tbody", "tr", "th", "td"]);
        sanitizer.AllowedAttributes.UnionWith(["href", "target", "rel", "title", "colspan", "rowspan", "src", "alt", "width", "height"]);
        sanitizer.AllowedSchemes.UnionWith(["http", "https"]);
        return sanitizer.Sanitize(content.Trim());
    }
    private void AddAudit(Guid actor, Guid id, string action, object? payload) => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "Campaign", EntityId = id.ToString(), ActionName = action, PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload), CreatedAtUtc = DateTime.UtcNow });
    private Guid AddCampaignHistory(Guid id, string? from, string to, string? note, Guid actor, DateTime now) { var historyId = Guid.NewGuid(); db.CampaignStatusHistories.Add(new CampaignStatusHistoryEntity { Id = historyId, CampaignId = id, FromStatus = from, ToStatus = to, Note = note, ActorUserId = actor, CreatedAtUtc = now }); return historyId; }
    private void AddDeliverableHistory(Guid campaignId, Guid deliverableId, string action, CampaignDeliverableInput? from, CampaignDeliverableInput? to, Guid actor, DateTime now) => db.CampaignDeliverableHistories.Add(new CampaignDeliverableHistoryEntity { Id = Guid.NewGuid(), CampaignId = campaignId, DeliverableId = deliverableId, Action = action, FromDeliverableType = from?.DeliverableType, FromQuantity = from?.Quantity, FromDueDateUtc = from?.DueDateUtc, ToDeliverableType = to?.DeliverableType, ToQuantity = to?.Quantity, ToDueDateUtc = to?.DueDateUtc, ActorUserId = actor, CreatedAtUtc = now });
    private sealed class CampaignRow
    {
        public Guid Id { get; init; }
        public Guid BrandProfileId { get; init; }
        public Guid BrandUserId { get; init; }
        public string Title { get; init; } = "";
        public string? AvatarUrl { get; init; }
        public string? BannerUrl { get; init; }
        public string BrandName { get; init; } = "";
        public decimal BudgetAmount { get; init; }
        public string PackageCode { get; init; } = "";
        public string Status { get; init; } = "";
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public int DesiredKocCount { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public bool IsVisible { get; init; }
        public int ApplicationCount { get; init; }
        public int SelectedKocCount { get; init; }
        public decimal FundedBudget { get; init; }
        public bool HasWorkStarted { get; init; }
        public bool IsShortlistFinalized { get; init; }
        public bool RequiresBooking { get; init; }
        public bool RequiresShipment { get; init; }
        public string DeliverableType { get; init; } = "";
    }
}
