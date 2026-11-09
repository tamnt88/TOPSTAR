using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Campaigns;

public sealed class CampaignWorkspaceService(TopstarDbContext db) : ICampaignWorkspaceService
{
    public static string? PreparationError(Topstar.Infrastructure.Entities.CampaignEntity campaign)
    {
        if (!campaign.CheckoutAtCreation) return null;
        if (CampaignExecutionModes.IsOnsite(campaign.ExecutionMode) &&
            new[]{campaign.VenueName,campaign.VenueAddress,campaign.ContactName,campaign.ContactPhone,campaign.ExecutionSchedule}.Any(string.IsNullOrWhiteSpace))
            return "Vui lòng bổ sung địa điểm, người liên hệ, số điện thoại và lịch thực hiện tại Thông tin triển khai trước khi mở công việc.";
        if (CampaignExecutionModes.IsOnsite(campaign.ExecutionMode) && !System.Text.RegularExpressions.Regex.IsMatch(campaign.ContactPhone?.Trim() ?? "", @"^[0-9+\s().-]{8,20}$"))
            return "Vui lòng kiểm tra số điện thoại liên hệ tại Thông tin triển khai trước khi mở công việc.";
        if (campaign.ExecutionMode == CampaignExecutionModes.LivestreamOnline && string.IsNullOrWhiteSpace(campaign.ExecutionSchedule))
            return "Vui lòng bổ sung lịch livestream tại Thông tin triển khai trước khi mở công việc.";
        return null;
    }

    public async Task<CampaignWorkLaunchResult> OpenWorkAsync(Guid brandUserId, Guid campaignId, Guid? participationId, bool requireFullTeam, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var campaign = await (from c in db.Campaigns
                              join b in db.BrandProfiles on c.BrandProfileId equals b.Id
                              where c.Id == campaignId && b.UserId == brandUserId
                              select c).FirstOrDefaultAsync(ct);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch hoặc Brand không có quyền thao tác.", []);
        if (campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress)) return new(false, "Chiến dịch không ở trạng thái có thể triển khai.", []);

        var preparationError = PreparationError(campaign);
        if (preparationError is not null) return new(false, preparationError, []);

        var confirmed = await db.Participations.Where(x => x.CampaignId == campaignId && x.Status == ParticipationStatuses.Confirmed).OrderBy(x => x.ConfirmedAtUtc).ToListAsync(ct);
        if (confirmed.Count == 0) return new(false, "Chưa có KOC nào đồng ý tham gia chiến dịch.", []);
        if (requireFullTeam && confirmed.Count < campaign.DesiredKocCount)
            return new(false, $"Chiến dịch yêu cầu đủ đội hình. Hiện có {confirmed.Count}/{campaign.DesiredKocCount} KOC đã đồng ý.", []);

        var targets = participationId.HasValue
            ? confirmed.Where(x => x.Id == participationId.Value).ToList()
            : confirmed;
        if (targets.Count == 0) return new(false, "KOC chưa đồng ý tham gia hoặc không thuộc chiến dịch này.", []);
        if (requireFullTeam && participationId.HasValue) return new(false, "Chế độ đủ đội hình phải mở công việc đồng loạt.", []);

        var targetIds = targets.Select(x => x.Id).ToArray();
        var paidFundingIds = await db.CampaignFundings.AsNoTracking()
            .Where(x => x.CampaignId == campaignId && (x.Status == CampaignFundingStatuses.Paid || x.Status == CampaignFundingStatuses.PartiallyReleased || x.Status == CampaignFundingStatuses.Released))
            .Select(x => x.Id).ToArrayAsync(ct);
        var fundedIds = await db.CampaignFundingAllocations.AsNoTracking()
            .Where(x => paidFundingIds.Contains(x.FundingId) && targetIds.Contains(x.ParticipationId) && x.Status == "Active" && x.Amount > 0)
            .Select(x => x.ParticipationId).Distinct().ToArrayAsync(ct);
        var missingFunding = targetIds.Except(fundedIds).Count();
        if (requireFullTeam && missingFunding > 0) return new(false, $"Còn {missingFunding} KOC chưa có thù lao đã ký quỹ. Chưa thể mở cho toàn đội hình.", []);
        if (participationId.HasValue && missingFunding > 0) return new(false, "Thù lao của KOC này chưa được ký quỹ nên chưa thể mở công việc.", []);
        if (!requireFullTeam && !participationId.HasValue)
            targets = targets.Where(x => fundedIds.Contains(x.Id)).ToList();
        if (targets.Count == 0)
            return new(false, $"Hiện có {confirmed.Count} KOC đã xác nhận nhưng chưa có khoản thù lao nào được ký quỹ. Vui lòng phân bổ thù lao và hoàn tất ký quỹ trước khi mở công việc.", []);

        var now = DateTime.UtcNow;
        var opened = targets.Where(x => !x.WorkOpenedAtUtc.HasValue).ToList();
        if (opened.Count == 0) return new(true, "Công việc đã được mở trước đó.", []);
        foreach (var participation in opened)
        {
            participation.WorkOpenedAtUtc = now;
            participation.WorkOpenedByUserId = brandUserId;
            participation.UpdatedAtUtc = now;
            db.ParticipationStatusHistories.Add(new() { Id = Guid.NewGuid(), ParticipationId = participation.Id, FromStatus = participation.Status, ToStatus = participation.Status, Note = "Brand mở công việc cho KOC", ActorUserId = brandUserId, CreatedAtUtc = now });
        }
        if (campaign.Status == CampaignStatuses.Published)
        {
            campaign.Status = CampaignStatuses.InProgress;
            campaign.UpdatedAtUtc = now;
            db.CampaignStatusHistories.Add(new() { Id = Guid.NewGuid(), CampaignId = campaignId, FromStatus = CampaignStatuses.Published, ToStatus = CampaignStatuses.InProgress, Note = "Brand chủ động mở công việc cho KOC đã đủ điều kiện.", ActorUserId = brandUserId, CreatedAtUtc = now });
        }
        var kocRecipients = await (from p in db.Participations.AsNoTracking()
                                   join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
                                   where opened.Select(x => x.Id).Contains(p.Id)
                                   select new { p.Id, k.UserId }).ToListAsync(ct);
        foreach (var recipient in kocRecipients)
            db.Notifications.Add(NotificationFactory.Create(recipient.UserId, "Participation", "Brand đã mở công việc", $"Công việc của chiến dịch {campaign.Title} đã sẵn sàng. Bạn có thể bắt đầu thực hiện.", now, "Participation", recipient.Id, audienceRole: "KOC"));
        db.AuditLogs.Add(new() { ActorUserId = brandUserId, EntityName = "Campaign", EntityId = campaignId.ToString(), ActionName = requireFullTeam ? "OpenWorkForFullTeam" : participationId.HasValue ? "OpenWorkForKoc" : "OpenWorkForAcceptedKocs", PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { ParticipationIds = opened.Select(x => x.Id) }), CreatedAtUtc = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(true, $"Đã mở công việc cho {opened.Count} KOC.", opened.Select(x => x.Id).ToArray());
    }

    public async Task<CampaignWorkspace?> GetForBrandAsync(Guid brandUserId, Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await (from c in db.Campaigns.AsNoTracking()
                              join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                              where c.Id == campaignId && b.UserId == brandUserId
                              select new { c.Id, c.Title, BrandName = b.CompanyName, c.PackageCode, c.Status, c.BudgetAmount, c.StartDate, c.EndDate, c.DesiredKocCount, c.RequiresBooking, c.RequiresShipment })
            .FirstOrDefaultAsync(ct);
        if (campaign is null) return null;

        var finalizedAt = await db.Shortlists.AsNoTracking().Where(x => x.CampaignId == campaignId).Select(x => x.FinalizedAtUtc).FirstOrDefaultAsync(ct);
        var activeApplications = await db.Applications.AsNoTracking().CountAsync(x => x.CampaignId == campaignId &&
            (x.Status == ApplicationStatuses.Applied || x.Status == ApplicationStatuses.Shortlisted || x.Status == ApplicationStatuses.Confirmed), ct);
        var confirmedKocs = await db.Participations.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && (x.Status == "Confirmed" || x.Status == "Completed"), ct);
        var participationIds = await db.Participations.AsNoTracking().Where(x => x.CampaignId == campaignId).Select(x => x.Id).ToArrayAsync(ct);
        var fundingRows = await db.CampaignFundings.AsNoTracking().Where(x => x.CampaignId == campaignId).OrderByDescending(x => x.VersionNo)
            .Select(x => new { x.Id, x.Status, x.TotalPayableSnapshot, x.PlatformFeeSnapshot, x.GatewayFeeSnapshot, x.OrderNumber, x.DueAtUtc, x.PaidAtUtc, x.RefundedAmount }).ToListAsync(ct);
        var funding = fundingRows.FirstOrDefault();
        var paidFundings = fundingRows.Where(x => x.Status is CampaignFundingStatuses.Paid or CampaignFundingStatuses.PartiallyReleased or CampaignFundingStatuses.Released).ToList();
        var operationalFundingStatus = paidFundings.Count > 0 ? CampaignFundingStatuses.Paid : funding?.Status;
        var scheduleTotal = await db.Schedules.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId), ct);
        var schedulePending = await db.Schedules.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status != "Confirmed" && x.Status != "Cancelled", ct);
        var shipmentTotal = await db.Shipments.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId), ct);
        var shipmentIssues = await db.Shipments.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && (x.Status == "Issue" || x.Status == "Returned"), ct);
        var submissionTotal = await db.Submissions.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId), ct);
        var submissionPending = await db.Submissions.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "Submitted", ct);
        var submissionNotSubmitted = await db.Submissions.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "NotSubmitted", ct);
        var submissionRevisionPending = await db.Submissions.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.Status == "RevisionRequested", ct);
        var disputeOpen = await db.DisputeCases.AsNoTracking().CountAsync(x => participationIds.Contains(x.ParticipationId) && x.IsActive && x.Status != "Resolved" && x.Status != "Cancelled", ct);
        var finalized = confirmedKocs > 0;
        var phase = CampaignWorkspaceRules.Resolve(new(campaign.Status, finalized, operationalFundingStatus,
            campaign.StartDate, DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)), confirmedKocs,
            campaign.RequiresBooking, scheduleTotal, schedulePending, campaign.RequiresShipment, shipmentTotal,
            shipmentIssues, submissionNotSubmitted, submissionPending, submissionRevisionPending, disputeOpen));

        var packageCost = await db.BrandPackagePurchases.AsNoTracking()
            .Where(x => x.CampaignId == campaignId).Select(x => (decimal?)x.AmountSnapshot).FirstOrDefaultAsync(ct) ?? 0m;
        var paidFundingIds = paidFundings.Select(x=>x.Id).ToArray();
        var allocations = paidFundingIds.Length == 0
            ? new List<Topstar.Infrastructure.Entities.CampaignFundingAllocationEntity>()
            : await db.CampaignFundingAllocations.AsNoTracking().Where(x => paidFundingIds.Contains(x.FundingId)).ToListAsync(ct);
        var releasedAmount = allocations.Sum(x => x.AcceptedAmount);
        var escrowRemaining = paidFundings.Count > 0
            ? CampaignFinancialSummaryRules.EscrowRemaining(allocations.Sum(x=>x.Amount), releasedAmount, allocations.Sum(x => x.RefundedAmount))
            : 0m;
        var paidAmount = paidFundings.Sum(x=>x.TotalPayableSnapshot);
        var financial = new CampaignFinancialSummary(packageCost, campaign.BudgetAmount,
            paidFundings.Sum(x=>x.PlatformFeeSnapshot), paidFundings.Sum(x=>x.GatewayFeeSnapshot), paidAmount,
            escrowRemaining, releasedAmount, funding?.RefundedAmount ?? 0m,
            CampaignFinancialSummaryRules.ActualCost(packageCost, paidAmount, funding?.RefundedAmount ?? 0m));

        var journeys = await (from p in db.Participations.AsNoTracking()
                              join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
                              where p.CampaignId == campaignId
                              orderby k.DisplayName
                              select new KocJourneyItem(
                                  p.Id,
                                  k.DisplayName,
                                  p.Status,
                                  db.Schedules.Where(x => x.ParticipationId == p.Id).OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => x.Status).FirstOrDefault() ?? "NotCreated",
                                  db.Shipments.Where(x => x.ParticipationId == p.Id).OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => x.Status).FirstOrDefault() ?? "NotCreated",
                                  db.Submissions.Where(x => x.ParticipationId == p.Id).OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => x.Status).FirstOrDefault() ?? "NotSubmitted",
                                  db.Submissions.Where(x => x.ParticipationId == p.Id).Join(db.CampaignDeliverables, s => s.DeliverableId, d => d.Id, (s, d) => d.DueDateUtc).OrderBy(x => x).FirstOrDefault(),
                                  db.Submissions.Any(x => x.ParticipationId == p.Id && x.Status == "Submitted") ? "Brand" : "KOC",
                                  db.Submissions.Any(x => x.ParticipationId == p.Id && x.Status == "Submitted") ? "Duyệt nội dung" :
                                  db.Shipments.Any(x => x.ParticipationId == p.Id && (x.Status == "Issue" || x.Status == "Returned")) ? "Xử lý giao nhận" :
                                  !db.Shipments.Any(x => x.ParticipationId == p.Id) && campaign.RequiresShipment ? "Tạo vận đơn" :
                                  !db.Schedules.Any(x => x.ParticipationId == p.Id) && campaign.RequiresBooking ? "Đề xuất lịch" : "Theo dõi thực hiện") { WorkOpenedAtUtc = p.WorkOpenedAtUtc })
            .ToListAsync(ct);
        var fundedParticipationIds = allocations
            .Where(x => x.Status == "Active" && x.Amount > 0)
            .Select(x => x.ParticipationId)
            .ToHashSet();
        journeys = journeys
            .Select(x => x with { IsFunded = fundedParticipationIds.Contains(x.ParticipationId) })
            .ToList();

        return new(campaign.Id, campaign.Title, campaign.BrandName, campaign.PackageCode, campaign.Status,
            campaign.BudgetAmount, campaign.StartDate, campaign.EndDate, campaign.DesiredKocCount, activeApplications,
            confirmedKocs, finalized, operationalFundingStatus, funding?.TotalPayableSnapshot ?? 0, funding?.OrderNumber,
            funding?.DueAtUtc, funding?.PaidAtUtc, campaign.RequiresBooking, scheduleTotal, schedulePending,
            campaign.RequiresShipment, shipmentTotal, shipmentIssues, submissionTotal, submissionPending,
            submissionNotSubmitted, submissionRevisionPending, disputeOpen,
            phase.Code, phase.Label, phase.Description, phase.NextAction, phase.NextPage, phase.NextSubPage,
            financial, journeys);
    }
}
