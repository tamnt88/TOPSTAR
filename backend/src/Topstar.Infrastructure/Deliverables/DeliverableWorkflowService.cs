using Topstar.Core.Common.Pagination;
using System.Text.Json;
using System.Data;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Deliverables;
using Topstar.Application.Billing;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Deliverables;

public sealed class DeliverableWorkflowService(TopstarDbContext db) : IDeliverableWorkflowService
{
    public async Task<IReadOnlyList<KocDeliverableTask>> GetKocTasksAsync(Guid kocUserId, CancellationToken cancellationToken = default)
    {
        var page = await GetKocTaskPageAsync(kocUserId, new(null, null, null, "due", 1, 50), cancellationToken);
        return page.Items;
    }

    public async Task<KocDeliverableTaskPage> GetKocTaskPageAsync(Guid kocUserId, KocDeliverableTaskQuery request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = PaginationDefaults.Normalize(request.PageSize);
        var now = DateTime.UtcNow;
        var baseQuery = from submission in db.Submissions.AsNoTracking()
                  join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
                  join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                  join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                  join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                  join deliverable in db.CampaignDeliverables.AsNoTracking() on submission.DeliverableId equals deliverable.Id
                   let fundingReady = (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                         join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                         where allocation.ParticipationId == participation.Id
                            && (funding.Status == CampaignFundingStatuses.Paid
                                || funding.Status == CampaignFundingStatuses.PartiallyReleased
                                || funding.Status == CampaignFundingStatuses.Released)
                         select allocation.Id).Any()
                   where koc.UserId == kocUserId && submission.IsVisible && (campaign.Status == CampaignStatuses.Published || campaign.Status == CampaignStatuses.InProgress || campaign.Status == CampaignStatuses.Completed)
                   select new { Submission = submission, Campaign = campaign, Brand = brand, Deliverable = deliverable, FundingReady = fundingReady, WorkOpened = participation.WorkOpenedAtUtc.HasValue };

        var notSubmittedCount = await baseQuery.CountAsync(x => x.WorkOpened && x.FundingReady && x.Campaign.Status == CampaignStatuses.InProgress && x.Submission.Status == SubmissionStatuses.NotSubmitted, cancellationToken);
        var needRevisionCount = await baseQuery.CountAsync(x => x.WorkOpened && x.FundingReady && x.Campaign.Status == CampaignStatuses.InProgress && x.Submission.Status == SubmissionStatuses.NeedRevision, cancellationToken);
        var submittedCount = await baseQuery.CountAsync(x => x.WorkOpened && x.FundingReady && x.Campaign.Status == CampaignStatuses.InProgress && x.Submission.Status == SubmissionStatuses.Submitted, cancellationToken);
        var approvedCount = await baseQuery.CountAsync(x => x.Submission.Status == SubmissionStatuses.Approved, cancellationToken);
        var overdueCount = await baseQuery.CountAsync(x =>
            x.Deliverable.DueDateUtc.HasValue && x.Deliverable.DueDateUtc < now &&
            (x.Submission.Status == SubmissionStatuses.NotSubmitted || x.Submission.Status == SubmissionStatuses.NeedRevision), cancellationToken);

        var unfilteredCount = await baseQuery.CountAsync(cancellationToken);
        var filtered = baseQuery;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            filtered = filtered.Where(x => x.Campaign.Title.Contains(search) || x.Brand.CompanyName.Contains(search) || x.Deliverable.DeliverableType.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
            filtered = filtered.Where(x => x.Submission.Status == request.Status);
        if (request.Due == "overdue")
            filtered = filtered.Where(x => x.Deliverable.DueDateUtc.HasValue && x.Deliverable.DueDateUtc < now && (x.Submission.Status == SubmissionStatuses.NotSubmitted || x.Submission.Status == SubmissionStatuses.NeedRevision));
        else if (request.Due == "seven_days")
            filtered = filtered.Where(x => x.Deliverable.DueDateUtc.HasValue && x.Deliverable.DueDateUtc >= now && x.Deliverable.DueDateUtc <= now.AddDays(7) &&
                (x.Submission.Status == SubmissionStatuses.NotSubmitted || x.Submission.Status == SubmissionStatuses.NeedRevision));
        else if (request.Due == "no_due")
            filtered = filtered.Where(x => !x.Deliverable.DueDateUtc.HasValue);

        filtered = request.Sort switch
        {
            "newest" => filtered.OrderByDescending(x => x.Submission.CreatedAtUtc).ThenBy(x => x.Campaign.Title).ThenBy(x => x.Submission.Id),
            "campaign" => filtered.OrderBy(x => x.Campaign.Title).ThenBy(x => x.Deliverable.DueDateUtc).ThenBy(x => x.Submission.Id),
            _ => filtered.OrderBy(x => x.Deliverable.DueDateUtc == null).ThenBy(x => x.Deliverable.DueDateUtc).ThenBy(x => x.Campaign.Title).ThenBy(x => x.Submission.Id)
        };

        var totalCount = await filtered.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, pageCount);
        var items = await filtered.Skip((page - 1) * pageSize).Take(pageSize)
                  .Select(x => new KocDeliverableTask(x.Submission.Id, x.Campaign.Id, x.Campaign.Title, x.Brand.CompanyName, x.Deliverable.DeliverableType,
                      x.Deliverable.Quantity, x.Deliverable.DueDateUtc, x.Submission.Status, x.Submission.SubmissionUrl, x.Submission.ReviewComment,
                      x.Submission.RevisionCount, x.Submission.SubmittedAtUtc, x.WorkOpened && x.FundingReady && x.Campaign.Status == CampaignStatuses.InProgress &&
                      (x.Submission.Status == SubmissionStatuses.NotSubmitted || x.Submission.Status == SubmissionStatuses.NeedRevision || x.Submission.Status == SubmissionStatuses.Submitted))
                  {
                      CampaignStatus = x.Campaign.Status,
                      FundingReady = x.FundingReady,
                      CampaignObjective = x.Campaign.Objective,
                      CampaignBudgetAmount = x.Campaign.BudgetAmount,
                      CampaignPackageCode = x.Campaign.PackageCode,
                      CampaignStartDate = x.Campaign.StartDate,
                      CampaignEndDate = x.Campaign.EndDate,
                      CampaignRequiresBooking = x.Campaign.RequiresBooking,
                      CampaignRequiresShipment = x.Campaign.RequiresShipment,
                      SubmissionNote = x.Submission.SubmissionNote,
                      AttachmentUrl = x.Submission.AttachmentUrl,
                      AttachmentName = x.Submission.AttachmentName
                  })
                  .ToListAsync(cancellationToken);
        var sanitizer = CreateCampaignHtmlSanitizer();
        items = items.Select(x => x with
        {
            CampaignObjective = string.IsNullOrWhiteSpace(x.CampaignObjective)
                ? null
                : sanitizer.Sanitize(x.CampaignObjective)
        }).ToList();
        var withHistory = await AddHistoryAsync(items, x => x.SubmissionId, cancellationToken);
        return new(withHistory, totalCount, page, pageSize, pageCount, notSubmittedCount, needRevisionCount, submittedCount, approvedCount, overdueCount) { UnfilteredCount = unfilteredCount };
    }

    private static HtmlSanitizer CreateCampaignHtmlSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.UnionWith(["h2", "h3", "h4", "p", "br", "ul", "ol", "li", "strong", "em", "u", "s", "blockquote", "pre", "code", "a", "figure", "figcaption", "img", "table", "thead", "tbody", "tr", "th", "td"]);
        sanitizer.AllowedAttributes.UnionWith(["href", "target", "rel", "src", "alt", "title", "width", "height", "colspan", "rowspan"]);
        sanitizer.AllowedSchemes.UnionWith(["http", "https"]);
        return sanitizer;
    }

    public async Task<IReadOnlyList<BrandSubmissionItem>> GetBrandSubmissionsAsync(Guid brandUserId, Guid? campaignId = null, CancellationToken cancellationToken = default)
    {
        var items = await (from submission in db.Submissions.AsNoTracking()
                  join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
                  join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                  join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                  join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                  join deliverable in db.CampaignDeliverables.AsNoTracking() on submission.DeliverableId equals deliverable.Id
                  where brand.UserId == brandUserId && submission.IsVisible && (campaign.Status == CampaignStatuses.InProgress || campaign.Status == CampaignStatuses.Completed) && (!campaignId.HasValue || campaign.Id == campaignId.Value)
                  orderby submission.Status == SubmissionStatuses.Submitted ? 0 : 1, deliverable.DueDateUtc, koc.DisplayName
                  select new BrandSubmissionItem(submission.Id, campaign.Id, campaign.Title, koc.DisplayName, deliverable.DeliverableType,
                      deliverable.Quantity, deliverable.DueDateUtc, submission.Status, submission.SubmissionUrl, submission.ReviewComment,
                      submission.RevisionCount, submission.SubmittedAtUtc, campaign.Status == CampaignStatuses.InProgress && submission.Status == SubmissionStatuses.Submitted)).ToListAsync(cancellationToken);
        var ids = items.Select(x => x.SubmissionId).ToArray();
        var histories = await LoadHistoriesAsync(ids, cancellationToken);
        return items.Select(x => x with { History = histories.GetValueOrDefault(x.SubmissionId, []) }).ToList();
    }

    public async Task<BrandCampaignExecutionSummary?> GetBrandCampaignSummaryAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await (from c in db.Campaigns.AsNoTracking()
                              join brand in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals brand.Id
                              where c.Id == campaignId && brand.UserId == brandUserId
                              select new { c.Id, c.Title, c.Status, c.RequiresBooking, c.RequiresShipment }).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return null;

        var statuses = await (from submission in db.Submissions.AsNoTracking()
                              join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
                              where participation.CampaignId == campaignId && submission.IsVisible
                              select submission.Status).ToListAsync(cancellationToken);
        var approved = statuses.Count(x => x == SubmissionStatuses.Approved);
        var rejected = statuses.Count(x => x == SubmissionStatuses.Rejected);
        var resolved = approved + rejected;
        var participationIds = await db.Participations.AsNoTracking().Where(x => x.CampaignId == campaignId).Select(x => x.Id).ToListAsync(cancellationToken);
        var bookingReady = !campaign.RequiresBooking || participationIds.Count > 0 && await db.Schedules.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "Confirmed", cancellationToken) == participationIds.Count;
        var shipmentReady = !campaign.RequiresShipment || participationIds.Count > 0 && await db.Shipments.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "Delivered", cancellationToken) == participationIds.Count;
        var noActiveDisputes = !await db.DisputeCases.AsNoTracking().AnyAsync(x => participationIds.Contains(x.ParticipationId) && x.IsActive, cancellationToken);
        var canComplete = CampaignCompletionRules.CanComplete(campaign.Status, statuses.Count, resolved, bookingReady, shipmentReady, noActiveDisputes);
        var hint = campaign.Status == CampaignStatuses.Completed
            ? "Chiến dịch đã hoàn tất. Nội dung hiện ở chế độ chỉ xem."
            : statuses.Count == 0
                ? "Chiến dịch chưa có nội dung để nghiệm thu."
                : !bookingReady
                    ? "Còn lịch làm việc chưa được xác nhận."
                : !shipmentReady
                    ? "Còn đơn hàng chưa được KOC xác nhận đã nhận."
                : !noActiveDisputes
                    ? "Chiến dịch đang có tranh chấp chưa được giải quyết."
                : canComplete
                    ? "Tất cả nội dung đã có kết quả duyệt. Doanh nghiệp có thể hoàn tất chiến dịch."
                    : $"Còn {statuses.Count - resolved} nội dung chưa có kết quả duyệt.";
        return new(campaign.Id, campaign.Title, campaign.Status, statuses.Count, resolved, approved, rejected, canComplete, hint);
    }

    public async Task<DeliverableResult> SubmitAsync(Guid kocUserId, Guid submissionId, string? submissionUrl, string? note, string? attachmentUrl, string? attachmentName, CancellationToken cancellationToken = default)
    {
        var url = submissionUrl?.Trim();
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        attachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl) ? null : attachmentUrl.Trim();
        attachmentName = string.IsNullOrWhiteSpace(attachmentName) ? null : attachmentName.Trim();
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(attachmentUrl))
            return new(false, "Vui lòng cung cấp link nội dung hoặc tải lên ít nhất một tệp.");
        if (note is null) return new(false, "Ghi chú gửi doanh nghiệp là bắt buộc.");
        if (!string.IsNullOrWhiteSpace(url) && (url.Length > 1000 || !Uri.TryCreate(url, UriKind.Absolute, out var submittedUrl) || submittedUrl.Scheme is not ("http" or "https")))
            return new(false, "Link nội dung phải là URL http/https hợp lệ.");
        if (note?.Length > 2000) return new(false, "Ghi chú không được vượt quá 2.000 ký tự.");
        if (attachmentUrl?.Length > 1000 || attachmentName?.Length > 260) return new(false, "Thông tin tệp đính kèm không hợp lệ.");
        var row = await (from submission in db.Submissions
                         join participation in db.Participations on submission.ParticipationId equals participation.Id
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where submission.Id == submissionId && koc.UserId == kocUserId && submission.IsVisible
                         select new { Submission = submission, ParticipationId = participation.Id, participation.WorkOpenedAtUtc, Campaign = campaign, BrandUserId = brand.UserId }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy đầu việc hoặc bạn không có quyền cập nhật.");
        if (!row.WorkOpenedAtUtc.HasValue) return new(false, "Brand chưa mở công việc này cho bạn.");
        if (row.Campaign.Status != CampaignStatuses.InProgress) return new(false, "Chỉ có thể nộp nội dung khi chiến dịch đang thực hiện.");
        if (!await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                    join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                    where allocation.ParticipationId == row.ParticipationId
                       && (funding.Status == CampaignFundingStatuses.Paid
                           || funding.Status == CampaignFundingStatuses.PartiallyReleased
                           || funding.Status == CampaignFundingStatuses.Released)
                    select allocation.Id).AnyAsync(cancellationToken))
            return new(false, "Khoản thù lao của KOC này chưa được ký quỹ nên chưa thể nộp nội dung.");
        if (!SubmissionTransitions.CanSubmit(row.Submission.Status)) return new(false, "Đầu việc này không ở trạng thái cho phép nộp bài.");

        var now = DateTime.UtcNow;
        var isReplacement = row.Submission.Status == SubmissionStatuses.Submitted;
        if (row.Submission.Status is SubmissionStatuses.NeedRevision or SubmissionStatuses.Submitted) row.Submission.RevisionCount++;
        row.Submission.Status = SubmissionStatuses.Submitted;
        row.Submission.SubmissionUrl = url;
        row.Submission.SubmissionNote = note;
        row.Submission.AttachmentUrl = attachmentUrl;
        row.Submission.AttachmentName = attachmentName;
        row.Submission.ReviewComment = null;
        row.Submission.SubmittedAtUtc = now;
        row.Submission.ReviewedAtUtc = null;
        row.Submission.UpdatedAtUtc = now;
        db.SubmissionHistories.Add(new SubmissionHistoryEntity { Id = Guid.NewGuid(), SubmissionId = submissionId, EventType = isReplacement ? "Replaced" : "Submitted", RevisionNo = row.Submission.RevisionCount, Status = SubmissionStatuses.Submitted, SubmissionUrl = url, SubmissionNote = note, AttachmentUrl = attachmentUrl, AttachmentName = attachmentName, ActorUserId = kocUserId, CreatedAtUtc = now });
        Notify(row.BrandUserId, "BRAND", "Submission", isReplacement ? "KOC vừa thay thế bài nộp" : "KOC vừa nộp nội dung", $"{(isReplacement ? "Phiên bản mới" : "Có nội dung mới")} cần duyệt trong chiến dịch {row.Campaign.Title}.", "Submission", submissionId, now);
        Audit(kocUserId, submissionId, isReplacement ? "ReplaceSubmission" : "Submit", new { Url = url, Note = note, AttachmentUrl = attachmentUrl, AttachmentName = attachmentName, row.Submission.RevisionCount }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, isReplacement ? "Đã thay thế nội dung và gửi lại doanh nghiệp duyệt." : "Đã gửi nội dung cho doanh nghiệp duyệt.", submissionId);
    }

    public async Task<DeliverableResult> ReviewAsync(Guid brandUserId, Guid submissionId, string decision, string? comment, CancellationToken cancellationToken = default)
    {
        decision = decision?.Trim() ?? string.Empty;
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (!SubmissionTransitions.IsReviewDecision(decision)) return new(false, "Quyết định duyệt không hợp lệ.");
        if (decision is SubmissionStatuses.NeedRevision or SubmissionStatuses.Rejected && comment is null)
            return new(false, "Vui lòng ghi rõ phản hồi khi yêu cầu sửa hoặc từ chối.");

        var row = await (from submission in db.Submissions
                         join participation in db.Participations on submission.ParticipationId equals participation.Id
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where submission.Id == submissionId && brand.UserId == brandUserId && submission.IsVisible
                         select new { Submission = submission, ParticipationId = participation.Id, Campaign = campaign, KocUserId = koc.UserId }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy bài nộp hoặc bạn không có quyền duyệt.");
        if (row.Campaign.Status != CampaignStatuses.InProgress) return new(false, "Chỉ có thể duyệt nội dung khi chiến dịch đang thực hiện.");
        if (!SubmissionTransitions.CanReview(row.Submission.Status)) return new(false, "Chỉ bài đang chờ duyệt mới được xử lý.");

        var now = DateTime.UtcNow;
        row.Submission.Status = decision;
        row.Submission.ReviewComment = comment;
        row.Submission.ReviewedAtUtc = now;
        row.Submission.UpdatedAtUtc = now;
        if (decision == SubmissionStatuses.Approved)
            await ReleaseParticipationEarningIfEligibleAsync(row.ParticipationId, brandUserId, now, cancellationToken);
        db.SubmissionHistories.Add(new SubmissionHistoryEntity { Id = Guid.NewGuid(), SubmissionId = submissionId, EventType = "Reviewed", RevisionNo = row.Submission.RevisionCount, Status = decision, SubmissionUrl = row.Submission.SubmissionUrl, ReviewComment = comment, SubmissionNote = row.Submission.SubmissionNote, AttachmentUrl = row.Submission.AttachmentUrl, AttachmentName = row.Submission.AttachmentName, ActorUserId = brandUserId, CreatedAtUtc = now });
        var title = decision == SubmissionStatuses.Approved ? "Nội dung đã được duyệt" : decision == SubmissionStatuses.NeedRevision ? "Nội dung cần chỉnh sửa" : "Nội dung bị từ chối";
        Notify(row.KocUserId, "KOC", "SubmissionReview", title, $"Brand đã phản hồi nội dung trong chiến dịch {row.Campaign.Title}.", "Submission", submissionId, now);
        Audit(brandUserId, submissionId, "Review", new { Decision = decision, Comment = comment }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, decision == SubmissionStatuses.Approved ? "Đã duyệt nội dung." : decision == SubmissionStatuses.NeedRevision ? "Đã gửi yêu cầu chỉnh sửa." : "Đã từ chối nội dung.", submissionId);
    }

    public async Task<SensitiveDataRedactionResult> RedactSensitiveDataAsync(Guid kocUserId, Guid historyId, bool redactLink, bool redactNote, bool redactAttachment, string? reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim();
        if (!redactLink && !redactNote && !redactAttachment)
            return new(false, "Vui lòng chọn ít nhất một dữ liệu cần ẩn.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10 || reason.Length > 500)
            return new(false, "Vui lòng nhập lý do xử lý từ 10 đến 500 ký tự.");

        var row = await (from history in db.SubmissionHistories
                         join submission in db.Submissions on history.SubmissionId equals submission.Id
                         join participation in db.Participations on submission.ParticipationId equals participation.Id
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         where history.Id == historyId && koc.UserId == kocUserId && submission.IsVisible
                         select new { History = history, Submission = submission }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy phiên bản hoặc bạn không có quyền xử lý dữ liệu này.");
        if ((redactLink && string.IsNullOrWhiteSpace(row.History.SubmissionUrl)) ||
            (redactNote && string.IsNullOrWhiteSpace(row.History.SubmissionNote)) ||
            (redactAttachment && string.IsNullOrWhiteSpace(row.History.AttachmentUrl)))
            return new(false, "Một hoặc nhiều dữ liệu đã chọn không còn tồn tại trong phiên bản.");

        var storedUrl = redactAttachment ? row.History.AttachmentUrl : null;
        var oldUrl = row.History.SubmissionUrl;
        var oldNote = row.History.SubmissionNote;
        var oldAttachmentUrl = row.History.AttachmentUrl;
        var now = DateTime.UtcNow;
        if (redactLink && string.Equals(row.Submission.SubmissionUrl, oldUrl, StringComparison.Ordinal))
            row.Submission.SubmissionUrl = null;
        if (redactNote && string.Equals(row.Submission.SubmissionNote, oldNote, StringComparison.Ordinal))
            row.Submission.SubmissionNote = null;
        if (redactAttachment && string.Equals(row.Submission.AttachmentUrl, oldAttachmentUrl, StringComparison.OrdinalIgnoreCase))
        {
            row.Submission.AttachmentUrl = null;
            row.Submission.AttachmentName = null;
        }
        row.Submission.UpdatedAtUtc = now;
        if (redactLink) row.History.SubmissionUrl = null;
        if (redactNote) row.History.SubmissionNote = null;
        if (redactAttachment)
        {
            row.History.AttachmentUrl = null;
            row.History.AttachmentName = null;
        }
        if (string.IsNullOrWhiteSpace(row.Submission.SubmissionUrl) && string.IsNullOrWhiteSpace(row.Submission.AttachmentUrl) &&
            row.Submission.Status == SubmissionStatuses.Submitted)
        {
            row.Submission.Status = SubmissionStatuses.NotSubmitted;
            row.Submission.SubmittedAtUtc = null;
            row.Submission.ReviewComment = null;
            row.Submission.ReviewedAtUtc = null;
        }
        db.SubmissionHistories.Add(new SubmissionHistoryEntity
        {
            Id = Guid.NewGuid(),
            SubmissionId = row.Submission.Id,
            EventType = "SensitiveDataRedacted",
            RevisionNo = row.History.RevisionNo,
            Status = row.Submission.Status,
            SubmissionNote = "KOC đã ẩn dữ liệu nhạy cảm khỏi phiên bản này.",
            ActorUserId = kocUserId,
            CreatedAtUtc = now
        });
        Audit(kocUserId, row.Submission.Id, "RedactSubmissionSensitiveData", new { HistoryId = historyId, row.History.RevisionNo, RedactLink = redactLink, RedactNote = redactNote, RedactAttachment = redactAttachment, Reason = reason }, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Chưa thể ẩn dữ liệu. Vui lòng chạy script bảo mật lịch sử mới rồi thử lại.");
        }
        return new(true, "Đã ẩn dữ liệu nhạy cảm đã chọn khỏi bài nộp và lịch sử.", storedUrl);
    }

    public async Task<DeliverableResult> CompleteCampaignAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var campaign = await (from c in db.Campaigns
                              join brand in db.BrandProfiles on c.BrandProfileId equals brand.Id
                              where c.Id == campaignId && brand.UserId == brandUserId
                              select c).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy campaign hoặc bạn không có quyền hoàn tất.");
        if (campaign.Status != CampaignStatuses.InProgress) return new(false, campaign.Status == CampaignStatuses.Completed ? "Chiến dịch đã hoàn tất trước đó." : "Chỉ chiến dịch đang thực hiện mới có thể hoàn tất.");

        var hasPaidFunding = await db.CampaignFundings.AnyAsync(x => x.CampaignId == campaignId &&
            (x.Status == CampaignFundingStatuses.Paid || x.Status == CampaignFundingStatuses.PartiallyReleased || x.Status == CampaignFundingStatuses.Released), cancellationToken);
        if (!hasPaidFunding)
            return new(false, "Chiến dịch chưa được ký quỹ đầy đủ nên không thể nghiệm thu và tạo thu nhập KOC.");

        var statuses = await (from submission in db.Submissions.AsNoTracking()
                              join participation in db.Participations.AsNoTracking() on submission.ParticipationId equals participation.Id
                              where participation.CampaignId == campaignId && submission.IsVisible
                              select submission.Status).ToListAsync(cancellationToken);
        var resolvedCount = statuses.Count(SubmissionTransitions.IsResolved);
        if (!CampaignCompletionRules.CanComplete(campaign.Status, statuses.Count, resolvedCount))
            return new(false, statuses.Count == 0 ? "Chiến dịch chưa có nội dung để nghiệm thu." : $"Còn {statuses.Count - resolvedCount} nội dung chưa được duyệt hoặc từ chối.");

        var now = DateTime.UtcNow;
        var participationIds = await db.Participations.AsNoTracking().Where(x => x.CampaignId == campaignId).Select(x => x.Id).ToListAsync(cancellationToken);
        if (campaign.RequiresBooking && (participationIds.Count == 0 || await db.Schedules.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "Confirmed", cancellationToken) != participationIds.Count))
            return new(false, "Còn lịch làm việc chưa được hai bên xác nhận.");
        if (campaign.RequiresShipment && (participationIds.Count == 0 || await db.Shipments.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "Delivered", cancellationToken) != participationIds.Count))
            return new(false, "Còn đơn hàng chưa được KOC xác nhận đã nhận.");
        if (await db.DisputeCases.AsNoTracking().AnyAsync(x => participationIds.Contains(x.ParticipationId) && x.IsActive, cancellationToken))
            return new(false, "Chiến dịch đang có tranh chấp chưa được giải quyết. Vui lòng chờ bộ phận hỗ trợ xử lý.");

        campaign.Status = CampaignStatuses.Completed;
        campaign.UpdatedAtUtc = now;
        db.CampaignStatusHistories.Add(new CampaignStatusHistoryEntity { Id = Guid.NewGuid(), CampaignId = campaignId, FromStatus = CampaignStatuses.InProgress, ToStatus = CampaignStatuses.Completed, Note = "All submissions resolved", ActorUserId = brandUserId, CreatedAtUtc = now });
        var participations = await db.Participations.Where(x => x.CampaignId == campaignId).ToListAsync(cancellationToken);
        foreach (var participation in participations)
        {
            participation.Status = CampaignStatuses.Completed;
            participation.UpdatedAtUtc = now;
            db.ParticipationStatusHistories.Add(new ParticipationStatusHistoryEntity { Id = Guid.NewGuid(), ParticipationId = participation.Id, FromStatus = "Confirmed", ToStatus = CampaignStatuses.Completed, Note = "Campaign completed", ActorUserId = brandUserId, CreatedAtUtc = now });
        }
        var kocUserIds = await (from participation in db.Participations.AsNoTracking()
                                join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                                where participation.CampaignId == campaignId
                                select koc.UserId).Distinct().ToListAsync(cancellationToken);
        foreach (var kocUserId in kocUserIds)
            Notify(kocUserId, "KOC", "CampaignCompleted", "Campaign đã hoàn tất", $"Brand đã hoàn tất nghiệm thu chiến dịch {campaign.Title}.", "Campaign", campaignId, now);
        foreach (var participation in participations)
            await ReleaseParticipationEarningIfEligibleAsync(participation.Id, brandUserId, now, cancellationToken);
        Audit(brandUserId, campaignId, "CompleteCampaign", new { SubmissionCount = statuses.Count, ResolvedCount = resolvedCount }, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Trạng thái campaign vừa thay đổi. Vui lòng tải lại trang và kiểm tra đầu việc.");
        }
        return new(true, "Đã hoàn tất campaign và khóa các đầu việc.", campaignId);
    }

    private async Task ReleaseParticipationEarningIfEligibleAsync(Guid participationId, Guid actorUserId, DateTime now, CancellationToken cancellationToken)
    {
        if (await db.WalletTransactions.AnyAsync(x => x.ParticipationId == participationId && x.TransactionType == "CampaignEarning", cancellationToken)) return;

        var participation = await db.Participations.FirstAsync(x => x.Id == participationId, cancellationToken);
        var campaign = await db.Campaigns.AsNoTracking().FirstAsync(x => x.Id == participation.CampaignId, cancellationToken);
        var submissionStatuses = await db.Submissions.AsNoTracking()
            .Where(x => x.ParticipationId == participationId && x.IsVisible)
            .Select(x => new { x.Id, x.Status })
            .ToListAsync(cancellationToken);
        var trackedStatuses = db.ChangeTracker.Entries<SubmissionEntity>()
            .Where(x => x.Entity.ParticipationId == participationId)
            .ToDictionary(x => x.Entity.Id, x => x.Entity.Status);
        if (submissionStatuses.Count == 0 || submissionStatuses.Any(x => trackedStatuses.GetValueOrDefault(x.Id, x.Status) != SubmissionStatuses.Approved)) return;
        if (campaign.RequiresBooking && !await db.Schedules.AsNoTracking().AnyAsync(x => x.ParticipationId == participationId && x.Status == "Confirmed", cancellationToken)) return;
        if (campaign.RequiresShipment && !await db.Shipments.AsNoTracking().AnyAsync(x => x.ParticipationId == participationId && x.Status == "Delivered", cancellationToken)) return;
        if (await db.DisputeCases.AsNoTracking().AnyAsync(x => x.ParticipationId == participationId && x.IsActive, cancellationToken)) return;

        var fundedAllocation = await (from allocation in db.CampaignFundingAllocations
                                      join fundingRow in db.CampaignFundings on allocation.FundingId equals fundingRow.Id
                                      where allocation.ParticipationId == participationId && allocation.ReleasedAtUtc == null
                                         && (fundingRow.Status == CampaignFundingStatuses.Paid || fundingRow.Status == CampaignFundingStatuses.PartiallyReleased)
                                      orderby fundingRow.VersionNo descending
                                      select new { Allocation = allocation, Funding = fundingRow }).FirstOrDefaultAsync(cancellationToken);
        if (fundedAllocation is null) return;

        var wallet = new WalletTransactionEntity
        {
            Id = Guid.NewGuid(), KocProfileId = participation.KocProfileId, CampaignId = participation.CampaignId,
            ParticipationId = participationId, TransactionType = "CampaignEarning", Amount = fundedAllocation.Allocation.Amount,
            Status = WalletStatuses.Pending, Description = "Thù lao đã nghiệm thu theo KOC", CreatedByUserId = actorUserId, CreatedAtUtc = now
        };
        db.WalletTransactions.Add(wallet);
        db.WalletTransactionStatusHistories.Add(new() { Id = Guid.NewGuid(), WalletTransactionId = wallet.Id, ToStatus = WalletStatuses.Pending, Reason = "KOC đã hoàn thành và được nghiệm thu", ActorUserId = actorUserId, CreatedAtUtc = now });
        fundedAllocation.Allocation.AcceptedAmount = fundedAllocation.Allocation.Amount;
        fundedAllocation.Allocation.RefundedAmount = 0;
        fundedAllocation.Allocation.Status = "Accepted";
        fundedAllocation.Allocation.SettlementReason = "KOC đã hoàn thành toàn bộ đầu việc và được Brand nghiệm thu.";
        fundedAllocation.Allocation.ReleasedAtUtc = now;
        fundedAllocation.Allocation.WalletTransactionId = wallet.Id;
        var hasOtherUnreleasedAllocation = await db.CampaignFundingAllocations.AsNoTracking()
            .AnyAsync(x => x.FundingId == fundedAllocation.Funding.Id && x.Id != fundedAllocation.Allocation.Id && x.ReleasedAtUtc == null, cancellationToken);
        fundedAllocation.Funding.Status = hasOtherUnreleasedAllocation ? CampaignFundingStatuses.PartiallyReleased : CampaignFundingStatuses.Released;
        fundedAllocation.Funding.UpdatedAtUtc = now;
        fundedAllocation.Funding.UpdatedByUserId = actorUserId;
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = fundedAllocation.Funding.Id, AllocationId = fundedAllocation.Allocation.Id, EntryType = "EarningCreated", Amount = -fundedAllocation.Allocation.Amount, Note = participationId.ToString(), ActorUserId = actorUserId, CreatedAtUtc = now });

        if (participation.Status != CampaignStatuses.Completed)
        {
            var previousStatus = participation.Status;
            participation.Status = CampaignStatuses.Completed;
            participation.UpdatedAtUtc = now;
            db.ParticipationStatusHistories.Add(new() { Id = Guid.NewGuid(), ParticipationId = participationId, FromStatus = previousStatus, ToStatus = CampaignStatuses.Completed, Note = "KOC đã hoàn thành và được nghiệm thu", ActorUserId = actorUserId, CreatedAtUtc = now });
        }
        var kocUserId = await db.KocProfiles.AsNoTracking().Where(x => x.Id == participation.KocProfileId).Select(x => x.UserId).FirstAsync(cancellationToken);
        Notify(kocUserId, "KOC", "Wallet", "Thù lao đang chờ khả dụng", $"Thù lao {wallet.Amount:N0} ₫ đã được ghi nhận và đang trong thời gian chờ tranh chấp.", "WalletTransaction", wallet.Id, now);
    }

    private void Notify(Guid userId, string audienceRole, string type, string title, string body, string sourceType, Guid sourceId, DateTime now)
        => db.Notifications.Add(NotificationFactory.Create(userId,type,title,body,now,sourceType,sourceId,audienceRole:audienceRole));

    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now)
        => db.AuditLogs.Add(new AuditLogEntity { ActorUserId = actor, EntityName = "Submission", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });

    private async Task<IReadOnlyList<KocDeliverableTask>> AddHistoryAsync(IReadOnlyList<KocDeliverableTask> items, Func<KocDeliverableTask, Guid> idSelector, CancellationToken cancellationToken)
    {
        var histories = await LoadHistoriesAsync(items.Select(idSelector).ToArray(), cancellationToken);
        return items.Select(x => x with { History = histories.GetValueOrDefault(idSelector(x), []) }).ToList();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<SubmissionEventItem>>> LoadHistoriesAsync(Guid[] submissionIds, CancellationToken cancellationToken)
    {
        var rows = await db.SubmissionHistories.AsNoTracking().Where(x => submissionIds.Contains(x.SubmissionId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.SubmissionId).ToDictionary(g => g.Key, g => (IReadOnlyList<SubmissionEventItem>)g.Select(x => new SubmissionEventItem(x.Id, x.EventType, x.RevisionNo, x.Status, x.SubmissionUrl, x.ReviewComment, x.SubmissionNote, x.AttachmentUrl, x.AttachmentName, x.CreatedAtUtc)).ToList());
    }
}
