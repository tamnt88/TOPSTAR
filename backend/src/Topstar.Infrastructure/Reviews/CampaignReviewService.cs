using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Reviews;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Reviews;

public sealed class CampaignReviewService(TopstarDbContext db) : ICampaignReviewService
{
    public async Task<IReadOnlyList<BrandReviewTask>> GetBrandTasksAsync(Guid brandUserId, Guid? campaignId = null, CancellationToken cancellationToken = default)
        => await (from participation in db.Participations.AsNoTracking()
                  join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                  join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                  join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                  let review = db.Reviews.AsNoTracking().FirstOrDefault(x => x.CampaignId == campaign.Id && x.ReviewerUserId == brandUserId && x.RevieweeUserId == koc.UserId)
                  where brand.UserId == brandUserId && campaign.Status == "Completed" && participation.Status == "Completed"
                        && (!campaignId.HasValue || campaign.Id == campaignId.Value)
                  orderby review != null, campaign.UpdatedAtUtc descending, koc.DisplayName
                  select new BrandReviewTask(participation.Id, campaign.Id, campaign.Title, koc.DisplayName, review != null,
                      review == null ? null : review.Rating, review == null ? null : review.Comment, review == null ? null : review.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KocReviewTask>> GetKocTasksAsync(Guid kocUserId, CancellationToken cancellationToken = default)
        => await (from participation in db.Participations.AsNoTracking()
                  join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                  join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                  join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                  let review = db.Reviews.AsNoTracking().FirstOrDefault(x => x.CampaignId == campaign.Id && x.ReviewerUserId == kocUserId && x.RevieweeUserId == brand.UserId)
                  where koc.UserId == kocUserId && campaign.Status == "Completed" && participation.Status == "Completed"
                  orderby review != null, campaign.UpdatedAtUtc descending
                  select new KocReviewTask(participation.Id, campaign.Id, campaign.Title, brand.CompanyName, review != null,
                      review == null ? null : review.Rating, review == null ? null : review.Comment, review == null ? null : review.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<KocReviewTaskPage> GetKocReviewTaskPageAsync(Guid kocUserId, KocReviewTaskQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(query.PageSize);
        var page = Math.Max(1, query.Page);
        var search = query.Search?.Trim();
        var fromUtc = query.FromDate?.Date;
        var toUtcExclusive = query.ToDate?.Date.AddDays(1);

        var source =
            from participation in db.Participations.AsNoTracking()
            join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
            join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
            join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
            let review = db.Reviews.AsNoTracking().FirstOrDefault(x => x.CampaignId == campaign.Id && x.ReviewerUserId == kocUserId && x.RevieweeUserId == brand.UserId)
            where koc.UserId == kocUserId && campaign.Status == "Completed" && participation.Status == "Completed"
            select new { Participation = participation, Campaign = campaign, Brand = brand, Review = review };

        var pendingCount = await source.CountAsync(x => x.Review == null, cancellationToken);
        var completedCount = await source.CountAsync(x => x.Review != null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.Brand.CompanyName.Contains(search) || x.Campaign.Title.Contains(search));
        if (fromUtc.HasValue) source = source.Where(x => (x.Review != null ? x.Review.CreatedAtUtc : x.Campaign.UpdatedAtUtc) >= fromUtc.Value);
        if (toUtcExclusive.HasValue) source = source.Where(x => (x.Review != null ? x.Review.CreatedAtUtc : x.Campaign.UpdatedAtUtc) < toUtcExclusive.Value);
        if (query.Rating is >= 1 and <= 5) source = source.Where(x => x.Review != null && x.Review.Rating == query.Rating);
        source = query.State?.Trim().ToLowerInvariant() switch
        {
            "pending" => source.Where(x => x.Review == null),
            "completed" => source.Where(x => x.Review != null),
            _ => source
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, pageCount);
        var items = await source
            .OrderBy(x => x.Review != null)
            .ThenByDescending(x => x.Review != null ? x.Review.CreatedAtUtc : x.Campaign.UpdatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new KocReviewTask(
                x.Participation.Id, x.Campaign.Id, x.Campaign.Title, x.Brand.CompanyName, x.Review != null,
                x.Review == null ? null : x.Review.Rating,
                x.Review == null ? null : x.Review.Comment,
                x.Review == null ? null : x.Review.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new(items, totalCount, page, pageSize, pageCount, pendingCount, completedCount);
    }

    public Task<ReviewResult> ReviewKocAsync(Guid brandUserId, Guid participationId, byte rating, string? comment, CancellationToken cancellationToken = default)
        => CreateAsync(brandUserId, participationId, rating, comment, reviewerIsBrand: true, cancellationToken);

    public Task<ReviewResult> ReviewBrandAsync(Guid kocUserId, Guid participationId, byte rating, string? comment, CancellationToken cancellationToken = default)
        => CreateAsync(kocUserId, participationId, rating, comment, reviewerIsBrand: false, cancellationToken);

    public async Task<ReviewResult> UpdateBrandReviewAsync(Guid kocUserId, Guid participationId, byte rating, string? comment, string reason, CancellationToken cancellationToken = default)
    {
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        reason = reason?.Trim() ?? string.Empty;
        if (!ReviewRules.IsValidRating(rating)) return new(false, "Điểm đánh giá phải từ 1 đến 5.");
        if (comment?.Length > 2000) return new(false, "Nhận xét không được vượt quá 2.000 ký tự.");
        if (reason.Length is < 10 or > 500) return new(false, "Lý do chỉnh sửa phải từ 10 đến 500 ký tự.");
        var row = await (from participation in db.Participations
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where participation.Id == participationId
                         select new { campaign.Id, BrandUserId = brand.UserId }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy chiến dịch.");
        var review = await db.Reviews.SingleOrDefaultAsync(x => x.CampaignId == row.Id && x.ReviewerUserId == kocUserId && x.RevieweeUserId == row.BrandUserId, cancellationToken);
        if (review is null) return new(false, "Không tìm thấy đánh giá cần chỉnh sửa.");
        var editHours=await db.OperationalRuntimePolicies.AsNoTracking().Select(x=>x.ReviewEditHours).SingleAsync(cancellationToken);
        var deadline = review.CreatedAtUtc.AddHours(editHours);
        if (DateTime.UtcNow > deadline) return new(false, $"Đã hết thời hạn chỉnh sửa {editHours} giờ.");
        if (review.Status != "Completed" || await db.ReviewReplies.AnyAsync(x => x.ReviewId == review.Id, cancellationToken) || await db.ReviewAppeals.AnyAsync(x => x.ReviewId == review.Id, cancellationToken))
            return new(false, "Đánh giá đã có tương tác hoặc xử lý nên không thể chỉnh sửa.");
        var history = new ReviewRevisionHistoryEntity { Id = Guid.NewGuid(), ReviewId = review.Id, FromRating = review.Rating, ToRating = rating, FromComment = review.Comment, ToComment = comment, Reason = reason, ActorUserId = kocUserId, CreatedAtUtc = DateTime.UtcNow };
        review.Rating = rating; review.Comment = comment; review.UpdatedAtUtc = history.CreatedAtUtc;
        db.ReviewRevisionHistories.Add(history);
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = kocUserId, EntityName = "Review", EntityId = review.Id.ToString(), ActionName = "Revise", PayloadJson = JsonSerializer.Serialize(new { history.FromRating, history.ToRating, reason }), CreatedAtUtc = history.CreatedAtUtc });
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã cập nhật đánh giá và lưu lịch sử.", review.Id);
    }

    public async Task<IReadOnlyList<AdminReviewItem>> GetAdminReviewsAsync(CancellationToken cancellationToken = default)
    {
        var items = await (from review in db.Reviews.AsNoTracking()
                           join campaign in db.Campaigns.AsNoTracking() on review.CampaignId equals campaign.Id
                           join reviewer in db.Users.AsNoTracking() on review.ReviewerUserId equals reviewer.Id
                           join reviewee in db.Users.AsNoTracking() on review.RevieweeUserId equals reviewee.Id
                           join replyGroup in db.ReviewReplies.AsNoTracking() on review.Id equals replyGroup.ReviewId into replies
                           from reply in replies.DefaultIfEmpty()
                           orderby review.Status == "Completed" ? 0 : 1, review.CreatedAtUtc descending
                           select new AdminReviewItem(review.Id, campaign.Title, reviewer.Email, reviewee.Email, review.Rating, review.Comment, reply == null ? null : reply.Body, review.Status, review.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        var ids = items.Select(x => x.Id).ToArray();
        var histories = await db.ReviewStatusHistories.AsNoTracking().Where(x => ids.Contains(x.ReviewId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(x => x with { History = histories.Where(h => h.ReviewId == x.Id).Select(h => new ReviewStatusEvent(h.FromStatus, h.ToStatus, h.Reason, h.CreatedAtUtc)).ToList() }).ToList();
    }

    public async Task<IReadOnlyList<ReceivedReviewItem>> GetReceivedReviewsAsync(Guid userId, Guid? campaignId = null, CancellationToken cancellationToken = default)
        => await (from review in db.Reviews.AsNoTracking()
                  join campaign in db.Campaigns.AsNoTracking() on review.CampaignId equals campaign.Id
                  join reviewer in db.Users.AsNoTracking() on review.ReviewerUserId equals reviewer.Id
                  join replyGroup in db.ReviewReplies.AsNoTracking() on review.Id equals replyGroup.ReviewId into replies
                  from reply in replies.DefaultIfEmpty()
                  join appealGroup in db.ReviewAppeals.AsNoTracking() on review.Id equals appealGroup.ReviewId into appeals
                  from appeal in appeals.DefaultIfEmpty()
                  let brandName = db.BrandProfiles.AsNoTracking().Where(x => x.UserId == reviewer.Id).Select(x => x.CompanyName).FirstOrDefault()
                  let kocName = db.KocProfiles.AsNoTracking().Where(x => x.UserId == reviewer.Id).Select(x => x.DisplayName).FirstOrDefault()
                  where review.RevieweeUserId == userId && (!campaignId.HasValue || campaign.Id == campaignId.Value)
                  orderby review.CreatedAtUtc descending
                  select new ReceivedReviewItem(
                      review.Id, campaign.Title,
                      brandName ?? kocName ?? "Đối tác",
                      review.Rating, review.Comment, review.Status, review.CreatedAtUtc,
                      reply == null ? null : reply.Body, reply == null ? null : reply.CreatedAtUtc,
                      appeal == null ? null : appeal.Status, appeal == null ? null : appeal.Reason, appeal == null ? null : appeal.DecisionNote))
            .Take(100)
            .ToListAsync(cancellationToken);

    public async Task<ReceivedReviewPage> GetReceivedReviewPageAsync(Guid userId, ReceivedReviewQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(query.PageSize);
        var page = Math.Max(1, query.Page);
        var search = query.Search?.Trim();
        var state = query.State?.Trim().ToLowerInvariant();
        var fromUtc = query.FromDate?.Date;
        var toUtcExclusive = query.ToDate?.Date.AddDays(1);

        var source =
            from review in db.Reviews.AsNoTracking()
            join campaign in db.Campaigns.AsNoTracking() on review.CampaignId equals campaign.Id
            join reviewer in db.Users.AsNoTracking() on review.ReviewerUserId equals reviewer.Id
            join replyGroup in db.ReviewReplies.AsNoTracking() on review.Id equals replyGroup.ReviewId into replies
            from reply in replies.DefaultIfEmpty()
            join appealGroup in db.ReviewAppeals.AsNoTracking() on review.Id equals appealGroup.ReviewId into appeals
            from appeal in appeals.DefaultIfEmpty()
            let brandName = db.BrandProfiles.AsNoTracking().Where(x => x.UserId == reviewer.Id).Select(x => x.CompanyName).FirstOrDefault()
            let kocName = db.KocProfiles.AsNoTracking().Where(x => x.UserId == reviewer.Id).Select(x => x.DisplayName).FirstOrDefault()
            where review.RevieweeUserId == userId
            select new
            {
                Review = review,
                CampaignTitle = campaign.Title,
                ReviewerName = brandName ?? kocName ?? "Đối tác",
                Reply = reply,
                Appeal = appeal
            };

        var allCount = await source.CountAsync(cancellationToken);
        var unrepliedCount = await source.CountAsync(x => x.Reply == null, cancellationToken);
        var averageRating = allCount == 0 ? 0 : await source.AverageAsync(x => (double)x.Review.Rating, cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
            source = source.Where(x => x.ReviewerName.Contains(search) || x.CampaignTitle.Contains(search));
        if (fromUtc.HasValue) source = source.Where(x => x.Review.CreatedAtUtc >= fromUtc.Value);
        if (toUtcExclusive.HasValue) source = source.Where(x => x.Review.CreatedAtUtc < toUtcExclusive.Value);
        if (query.Rating is >= 1 and <= 5) source = source.Where(x => x.Review.Rating == query.Rating);
        source = state switch
        {
            "unreplied" => source.Where(x => x.Reply == null),
            "replied" => source.Where(x => x.Reply != null),
            "appeal-open" => source.Where(x => x.Appeal != null && x.Appeal.Status == "Open"),
            "appeal-closed" => source.Where(x => x.Appeal != null && x.Appeal.Status != "Open"),
            _ => source
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, pageCount);
        var items = await source.OrderByDescending(x => x.Review.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReceivedReviewItem(
                x.Review.Id, x.CampaignTitle, x.ReviewerName,
                x.Review.Rating, x.Review.Comment, x.Review.Status, x.Review.CreatedAtUtc,
                x.Reply == null ? null : x.Reply.Body, x.Reply == null ? null : x.Reply.CreatedAtUtc,
                x.Appeal == null ? null : x.Appeal.Status,
                x.Appeal == null ? null : x.Appeal.Reason,
                x.Appeal == null ? null : x.Appeal.DecisionNote))
            .ToListAsync(cancellationToken);

        return new(items, totalCount, page, pageSize, pageCount, allCount, unrepliedCount, averageRating);
    }

    public async Task<ReviewResult> ReplyAsync(Guid userId, Guid reviewId, string body, CancellationToken cancellationToken = default)
    {
        body = body?.Trim() ?? string.Empty;
        if (!ReviewRules.IsValidReply(body)) return new(false, "Phản hồi phải từ 2 đến 2.000 ký tự.");
        var review = await db.Reviews.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == reviewId && x.RevieweeUserId == userId, cancellationToken);
        if (review is null) return new(false, "Không tìm thấy đánh giá hoặc bạn không có quyền phản hồi.");
        var existingReply = await db.ReviewReplies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken);
        if (existingReply is not null)
            return new(true, "Phản hồi đã được ghi nhận.", existingReply.Id);

        var now = DateTime.UtcNow;
        var reply = new ReviewReplyEntity { Id = Guid.NewGuid(), ReviewId = reviewId, AuthorUserId = userId, Body = body, CreatedAtUtc = now };
        var reviewerAudienceRole = await db.BrandProfiles.AsNoTracking()
            .AnyAsync(x => x.UserId == review.ReviewerUserId, cancellationToken) ? "BRAND" : "KOC";
        db.ReviewReplies.Add(reply);
        db.Notifications.Add(NotificationFactory.Create(review.ReviewerUserId, "ReviewReply", "Đánh giá của bạn có phản hồi",
            "Đối tác đã phản hồi đánh giá trong lịch sử hợp tác.", now, "Review", reviewId, audienceRole: reviewerAudienceRole));
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = userId, EntityName = "ReviewReply", EntityId = reply.Id.ToString(), ActionName = "Create", PayloadJson = JsonSerializer.Serialize(new { reviewId }), CreatedAtUtc = now });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            existingReply = await db.ReviewReplies.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken);
            if (existingReply is not null)
                return new(true, "Phản hồi đã được ghi nhận.", existingReply.Id);
            return new(false, "Không thể lưu phản hồi. Vui lòng thử lại.");
        }
        return new(true, "Đã lưu phản hồi. Nội dung được giữ nguyên trong lịch sử.", reply.Id);
    }

    public async Task<ReviewResult> AppealAsync(Guid userId, Guid reviewId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (!ReviewRules.IsValidAppealReason(reason)) return new(false, "Căn cứ khiếu nại phải từ 10 đến 2.000 ký tự.");
        var review = await db.Reviews.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == reviewId && x.RevieweeUserId == userId, cancellationToken);
        if (review is null) return new(false, "Không tìm thấy đánh giá hoặc bạn không có quyền khiếu nại.");
        if (review.Status == "Hidden") return new(false, "Đánh giá đã được ẩn nên không cần tạo khiếu nại mới.");
        if (await db.ReviewAppeals.AnyAsync(x => x.ReviewId == reviewId, cancellationToken))
            return new(false, "Đánh giá này đã có hồ sơ khiếu nại.");

        var now = DateTime.UtcNow;
        var appeal = new ReviewAppealEntity { Id = Guid.NewGuid(), ReviewId = reviewId, AppellantUserId = userId, Reason = reason, Status = "Open", CreatedAtUtc = now };
        db.ReviewAppeals.Add(appeal);
        db.ReviewAppealHistories.Add(new ReviewAppealHistoryEntity { Id = Guid.NewGuid(), AppealId = appeal.Id, FromStatus = null, ToStatus = "Open", Note = reason, ActorUserId = userId, CreatedAtUtc = now });
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = userId, EntityName = "ReviewAppeal", EntityId = appeal.Id.ToString(), ActionName = "Create", PayloadJson = JsonSerializer.Serialize(new { reviewId }), CreatedAtUtc = now });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(false, "Hồ sơ khiếu nại vừa được tạo bởi một yêu cầu khác."); }
        return new(true, "Đã gửi khiếu nại tới Admin.", appeal.Id);
    }

    public async Task<ReviewResult> SupplementAppealAsync(Guid userId, Guid reviewId, string body, CancellationToken cancellationToken = default)
    {
        body = body?.Trim() ?? string.Empty;
        if (body.Length is < 10 or > 2000) return new(false, "Thông tin bổ sung phải từ 10 đến 2.000 ký tự.");
        var appeal = await db.ReviewAppeals.SingleOrDefaultAsync(x => x.ReviewId == reviewId && x.AppellantUserId == userId, cancellationToken);
        if (appeal is null) return new(false, "Không tìm thấy yêu cầu xem xét.");
        if (appeal.Status != "Open") return new(false, "Yêu cầu đã được xử lý nên không thể bổ sung.");
        var now = DateTime.UtcNow;
        db.ReviewAppealSupplements.Add(new ReviewAppealSupplementEntity { Id = Guid.NewGuid(), AppealId = appeal.Id, Body = body, ActorUserId = userId, CreatedAtUtc = now });
        db.ReviewAppealHistories.Add(new ReviewAppealHistoryEntity { Id = Guid.NewGuid(), AppealId = appeal.Id, FromStatus = "Open", ToStatus = "Open", Note = $"Bổ sung: {body}", ActorUserId = userId, CreatedAtUtc = now });
        appeal.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã bổ sung thông tin vào yêu cầu.", appeal.Id);
    }

    public async Task<ReviewResult> WithdrawAppealAsync(Guid userId, Guid reviewId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500) return new(false, "Lý do rút yêu cầu phải từ 10 đến 500 ký tự.");
        var appeal = await db.ReviewAppeals.SingleOrDefaultAsync(x => x.ReviewId == reviewId && x.AppellantUserId == userId, cancellationToken);
        if (appeal is null) return new(false, "Không tìm thấy yêu cầu xem xét.");
        if (appeal.Status != "Open") return new(false, "Chỉ được rút yêu cầu đang mở.");
        var now = DateTime.UtcNow;
        appeal.Status = "Withdrawn"; appeal.DecisionNote = reason; appeal.ResolvedByUserId = userId; appeal.ResolvedAtUtc = now; appeal.UpdatedAtUtc = now;
        db.ReviewAppealHistories.Add(new ReviewAppealHistoryEntity { Id = Guid.NewGuid(), AppealId = appeal.Id, FromStatus = "Open", ToStatus = "Withdrawn", Note = reason, ActorUserId = userId, CreatedAtUtc = now });
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = userId, EntityName = "ReviewAppeal", EntityId = appeal.Id.ToString(), ActionName = "Withdraw", PayloadJson = JsonSerializer.Serialize(new { reason }), CreatedAtUtc = now });
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã rút yêu cầu xem xét.", appeal.Id);
    }

    public async Task<IReadOnlyList<AdminReviewAppealItem>> GetAdminAppealsAsync(CancellationToken cancellationToken = default)
    {
        var items = await (from appeal in db.ReviewAppeals.AsNoTracking()
                           join review in db.Reviews.AsNoTracking() on appeal.ReviewId equals review.Id
                           join campaign in db.Campaigns.AsNoTracking() on review.CampaignId equals campaign.Id
                           join appellant in db.Users.AsNoTracking() on appeal.AppellantUserId equals appellant.Id
                           join reviewer in db.Users.AsNoTracking() on review.ReviewerUserId equals reviewer.Id
                           orderby appeal.Status == "Open" ? 0 : 1, appeal.CreatedAtUtc descending
                           select new AdminReviewAppealItem(appeal.Id, review.Id, campaign.Title, appellant.Email, reviewer.Email,
                               review.Rating, review.Comment, appeal.Reason, appeal.Status, appeal.DecisionNote, appeal.CreatedAtUtc))
            .Take(200).ToListAsync(cancellationToken);
        var ids = items.Select(x => x.Id).ToArray();
        var history = await db.ReviewAppealHistories.AsNoTracking().Where(x => ids.Contains(x.AppealId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return items.Select(x => x with { History = history.Where(h => h.AppealId == x.Id).Select(h => new ReviewAppealEvent(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList() }).ToList();
    }

    public async Task<ReviewResult> ResolveAppealAsync(Guid adminUserId, Guid appealId, string targetStatus, string decisionNote, CancellationToken cancellationToken = default)
    {
        decisionNote = decisionNote?.Trim() ?? string.Empty;
        if (decisionNote.Length is < 10 or > 2000) return new(false, "Kết luận phải từ 10 đến 2.000 ký tự.");
        var appeal = await db.ReviewAppeals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == appealId, cancellationToken);
        if (appeal is null) return new(false, "Không tìm thấy hồ sơ khiếu nại.");
        if (!ReviewRules.CanResolveAppeal(appeal.Status, targetStatus)) return new(false, "Hồ sơ không còn ở trạng thái có thể xử lý.");

        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var affected = await db.ReviewAppeals.Where(x => x.Id == appealId && x.Status == "Open")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, targetStatus).SetProperty(x => x.DecisionNote, decisionNote)
                .SetProperty(x => x.ResolvedByUserId, adminUserId).SetProperty(x => x.ResolvedAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        if (affected != 1) return new(false, "Hồ sơ vừa được xử lý bởi phiên khác.");
        if (targetStatus == "Accepted")
        {
            var review = await db.Reviews.AsNoTracking().SingleAsync(x => x.Id == appeal.ReviewId, cancellationToken);
            if (review.Status == "Completed")
            {
                await db.Reviews.Where(x => x.Id == review.Id && x.Status == "Completed")
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Hidden").SetProperty(x => x.IsVisible, false).SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
                db.ReviewStatusHistories.Add(new ReviewStatusHistoryEntity { Id = Guid.NewGuid(), ReviewId = review.Id, FromStatus = "Completed", ToStatus = "Hidden", Reason = $"Appeal accepted: {decisionNote}", ActorUserId = adminUserId, CreatedAtUtc = now });
            }
        }
        db.ReviewAppealHistories.Add(new ReviewAppealHistoryEntity { Id = Guid.NewGuid(), AppealId = appealId, FromStatus = "Open", ToStatus = targetStatus, Note = decisionNote, ActorUserId = adminUserId, CreatedAtUtc = now });
        db.Notifications.Add(NotificationFactory.Create(appeal.AppellantUserId, "ReviewAppeal", targetStatus == "Accepted" ? "Khiếu nại review được chấp nhận" : "Khiếu nại review bị từ chối", decisionNote, now, "Review", appeal.ReviewId));
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = adminUserId, EntityName = "ReviewAppeal", EntityId = appealId.ToString(), ActionName = "Resolve", PayloadJson = JsonSerializer.Serialize(new { targetStatus, decisionNote }), CreatedAtUtc = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, targetStatus == "Accepted" ? "Đã chấp nhận khiếu nại và ẩn review." : "Đã từ chối khiếu nại.", appealId);
    }

    public async Task<ReviewResult> ModerateAsync(Guid adminUserId, Guid reviewId, string targetStatus, string? reason, CancellationToken cancellationToken = default)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var review = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken);
        if (review is null) return new(false, "Không tìm thấy review.");
        if (!ReviewRules.CanModerate(review.Status, targetStatus)) return new(false, $"Không thể chuyển review từ {review.Status} sang {targetStatus}.");
        if (targetStatus == "Hidden" && reason is null) return new(false, "Vui lòng nhập lý do ẩn review.");
        if (reason?.Length > 500) return new(false, "Lý do không được vượt quá 500 ký tự.");
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.Reviews.Where(x => x.Id == reviewId && x.Status == review.Status)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, targetStatus).SetProperty(x => x.IsVisible, targetStatus == "Completed").SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        if (affected != 1) return new(false, "Review vừa được xử lý bởi phiên khác. Vui lòng tải lại.");
        db.ReviewStatusHistories.Add(new ReviewStatusHistoryEntity { Id = Guid.NewGuid(), ReviewId = reviewId, FromStatus = review.Status, ToStatus = targetStatus, Reason = reason, ActorUserId = adminUserId, CreatedAtUtc = now });
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = adminUserId, EntityName = "Review", EntityId = reviewId.ToString(), ActionName = "Moderate", PayloadJson = JsonSerializer.Serialize(new { Before = review.Status, After = targetStatus, Reason = reason }), CreatedAtUtc = now });
        var audienceRole = await (from c in db.Campaigns.AsNoTracking() join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id where c.Id == review.CampaignId && b.UserId == review.ReviewerUserId select b.Id).AnyAsync(cancellationToken) ? "BRAND" : "KOC";
        db.Notifications.Add(NotificationFactory.Create(review.ReviewerUserId,"ReviewModeration",targetStatus == "Hidden" ? "Đánh giá đã bị ẩn" : "Đánh giá đã được khôi phục",targetStatus == "Hidden" ? $"Admin đã ẩn đánh giá của bạn: {reason}" : "Admin đã khôi phục đánh giá của bạn.",now,"Review",reviewId,audienceRole:audienceRole));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, targetStatus == "Hidden" ? "Đã ẩn review." : "Đã khôi phục review.", reviewId);
    }

    private async Task<ReviewResult> CreateAsync(Guid reviewerUserId, Guid participationId, byte rating, string? comment, bool reviewerIsBrand, CancellationToken cancellationToken)
    {
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (!ReviewRules.IsValidRating(rating)) return new(false, "Điểm đánh giá phải từ 1 đến 5.");
        if (comment?.Length > 2000) return new(false, "Nội dung đánh giá không được vượt quá 2.000 ký tự.");

        var row = await (from participation in db.Participations.AsNoTracking()
                         join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                         join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                         where participation.Id == participationId
                            && (reviewerIsBrand ? brand.UserId == reviewerUserId : koc.UserId == reviewerUserId)
                         select new { Campaign = campaign, BrandUserId = brand.UserId, KocUserId = koc.UserId }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy lượt tham gia hoặc bạn không có quyền đánh giá.");
        if (!ReviewRules.CanReviewCampaign(row.Campaign.Status)) return new(false, "Chỉ được đánh giá sau khi campaign hoàn tất.");

        var revieweeUserId = reviewerIsBrand ? row.KocUserId : row.BrandUserId;
        if (await db.Reviews.AnyAsync(x => x.CampaignId == row.Campaign.Id && x.ReviewerUserId == reviewerUserId && x.RevieweeUserId == revieweeUserId, cancellationToken))
            return new(false, "Bạn đã đánh giá đối tác trong campaign này.");

        var now = DateTime.UtcNow;
        var review = new ReviewEntity { Id = Guid.NewGuid(), CampaignId = row.Campaign.Id, ReviewerUserId = reviewerUserId, RevieweeUserId = revieweeUserId, Rating = rating, Comment = comment, Status = "Completed", IsVisible = true, CreatedAtUtc = now };
        db.Reviews.Add(review);
        db.ReviewStatusHistories.Add(new ReviewStatusHistoryEntity { Id = Guid.NewGuid(), ReviewId = review.Id, FromStatus = null, ToStatus = "Completed", Reason = "Review submitted", ActorUserId = reviewerUserId, CreatedAtUtc = now });
        db.Notifications.Add(NotificationFactory.Create(revieweeUserId,"CampaignReview","Bạn có đánh giá mới",$"Bạn vừa nhận đánh giá sau chiến dịch {row.Campaign.Title}.",now,"Review",review.Id,audienceRole:reviewerIsBrand?"KOC":"BRAND"));
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = reviewerUserId, EntityName = "Review", EntityId = review.Id.ToString(), ActionName = "Create", PayloadJson = JsonSerializer.Serialize(new { row.Campaign.Id, RevieweeUserId = revieweeUserId, Rating = rating }), CreatedAtUtc = now });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Đánh giá đã được ghi nhận bởi một yêu cầu khác. Vui lòng tải lại trang.");
        }
        return new(true, "Cảm ơn bạn. Đánh giá đã được ghi nhận.", review.Id);
    }
}
