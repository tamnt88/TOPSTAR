using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Topstar.Application.Billing;
using Topstar.Application.Deliverables;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Finance;

public sealed class CampaignFundingService(TopstarDbContext db, ILogger<CampaignFundingService> logger) : ICampaignFundingService
{
    public async Task<CampaignFeePolicyItem> GetPolicyAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow; var p = await db.CampaignFeePolicies.AsNoTracking().Where(x => x.Code == "DEFAULT").OrderByDescending(x => x.IsActive && x.EffectiveAtUtc <= now).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.EffectiveAtUtc).ThenByDescending(x => x.Id).FirstAsync(ct);
        var h = await db.CampaignFeePolicyHistories.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100).Select(x => new CampaignFeePolicyHistoryItem(x.FeeRate, x.FixedFee, x.MinimumFee, x.MaximumFee, x.DualApprovalThreshold, x.Reason, x.CreatedAtUtc)).ToListAsync(ct);
        return new(p.Id, p.FeeRate, p.FixedFee, p.MinimumFee, p.MaximumFee, p.GatewayFeeRate, p.GatewayFeeBearer, p.DisputeHoldDays, p.PaymentDueDays, p.DualApprovalThreshold, p.RefundPolicy, p.EffectiveAtUtc, p.IsActive, h);
    }
    public async Task<CampaignFundingResult> UpdatePolicyAsync(Guid actor, decimal rate, decimal fixedFee, decimal? minimum, decimal? maximum, decimal gatewayRate, string bearer, int holdDays, int dueDays, decimal dualApprovalThreshold, string refund, DateTime effectiveAtUtc, bool isActive, string reason, CancellationToken ct = default)
    {
        var error = BillingRules.ValidatePolicy(rate, fixedFee, minimum, maximum, holdDays, dueDays); if (error is not null) return new(false, "Thông số chính sách phí không hợp lệ."); if (gatewayRate is < 0 or > 1 || bearer is not ("Brand" or "Topstar") || dualApprovalThreshold < 0) return new(false, "Phí cổng thanh toán, bên chịu phí hoặc ngưỡng duyệt không hợp lệ."); reason = reason?.Trim() ?? ""; refund = refund?.Trim() ?? ""; if (reason.Length < 10 || reason.Length > 500) return new(false, "Lý do thay đổi phải từ 10 đến 500 ký tự."); if (refund.Length < 10 || refund.Length > 1000) return new(false, "Chính sách hoàn tiền phải từ 10 đến 1.000 ký tự.");
        var now = DateTime.UtcNow;
        if (isActive && effectiveAtUtc <= now)
        {
            var superseded = await db.CampaignFeePolicies.Where(x => x.Code == "DEFAULT" && x.IsActive && x.EffectiveAtUtc <= now).ToListAsync(ct);
            foreach (var old in superseded) { old.IsActive = false; old.UpdatedAtUtc = now; old.UpdatedByUserId = actor; }
        }
        var p = new CampaignFeePolicyEntity { Id = Guid.NewGuid(), Code = "DEFAULT", FeeRate = rate, FixedFee = fixedFee, MinimumFee = minimum, MaximumFee = maximum, GatewayFeeRate = gatewayRate, GatewayFeeBearer = bearer, DisputeHoldDays = holdDays, PaymentDueDays = dueDays, DualApprovalThreshold = dualApprovalThreshold, RefundPolicy = refund, EffectiveAtUtc = effectiveAtUtc, IsActive = isActive, UpdatedByUserId = actor, CreatedAtUtc = now }; db.CampaignFeePolicies.Add(p);
        db.CampaignFeePolicyHistories.Add(new() { Id = Guid.NewGuid(), PolicyId = p.Id, FeeRate = rate, FixedFee = fixedFee, MinimumFee = minimum, MaximumFee = maximum, GatewayFeeRate = gatewayRate, GatewayFeeBearer = bearer, DisputeHoldDays = holdDays, PaymentDueDays = dueDays, DualApprovalThreshold = dualApprovalThreshold, RefundPolicy = refund, Reason = reason, ActorUserId = actor, CreatedAtUtc = now }); Audit(actor, p.Id, "UpdatePolicy", new { rate, fixedFee, minimum, maximum, gatewayRate, bearer, holdDays, dueDays, dualApprovalThreshold, reason }, now); await db.SaveChangesAsync(ct); return new(true, "Đã cập nhật chính sách phí chiến dịch.", p.Id);
    }
    public Task<IReadOnlyList<CampaignFundingItem>> GetForBrandAsync(Guid userId, CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null) => LoadAsync(userId, null, ct, window);
    public Task<IReadOnlyList<CampaignFundingItem>> GetForAdminAsync(CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null) => LoadAsync(null, null, ct, window);
    public async Task<CampaignFundingItem?> GetForBrandAsync(Guid userId, Guid campaignId, CancellationToken ct = default) => (await LoadAsync(userId, campaignId, ct)).FirstOrDefault();
    private async Task<IReadOnlyList<CampaignFundingItem>> LoadAsync(Guid? userId, Guid? campaignId, CancellationToken ct, Topstar.Application.Common.ListWindow? window = null)
    {
        var campaignQuery = from c in db.Campaigns.AsNoTracking()
                            join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                            let latestStatus = db.CampaignFundings.Where(f => f.CampaignId == c.Id)
                                .OrderByDescending(f => f.VersionNo).ThenByDescending(f => f.CreatedAtUtc)
                                .Select(f => f.Status).FirstOrDefault()
                            where (!userId.HasValue || b.UserId == userId) && (!campaignId.HasValue || c.Id == campaignId)
                            orderby c.CreatedAtUtc descending, c.Id
                            select new { c, b, Status = latestStatus ?? CampaignFundingStatuses.Draft };
        if (window is not null)
            window.Amounts = await (from f in db.CampaignFundings.AsNoTracking()
                                    join c in db.Campaigns.AsNoTracking() on f.CampaignId equals c.Id
                                    join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                                    where (!userId.HasValue || b.UserId == userId)
                                    group f by f.Status into groupByStatus
                                    select new { Status = groupByStatus.Key, Amount = groupByStatus.Sum(x => x.TotalPayableSnapshot) })
                .ToDictionaryAsync(x => x.Status, x => x.Amount, ct);
        var campaigns = await campaignQuery.ToWindowAsync(window, ct,
            q => q.Where(x => (window!.Search == null || x.c.Title.Contains(window.Search) || x.b.CompanyName.Contains(window.Search) || db.CampaignFundings.Any(f=>f.CampaignId==x.c.Id&&f.OrderNumber!=null&&f.OrderNumber.Contains(window.Search))) &&
                               (window.AllowedStatuses == null || window.AllowedStatuses.Contains(x.Status)) &&
                               (window.Status == null || x.Status == window.Status)), x => x.Status);
        var campaignKeys = campaigns.Select(x => x.c.Id).ToArray();
        var fundingRows = await db.CampaignFundings.AsNoTracking().Where(x => campaignKeys.Contains(x.CampaignId)).OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var rows = campaigns.Select(x => new { x.c, x.b, f = fundingRows.FirstOrDefault(f => f.CampaignId == x.c.Id) }).ToList();
        var finalizedCampaigns=await db.Shortlists.AsNoTracking().Where(x=>campaignKeys.Contains(x.CampaignId)&&x.FinalizedAtUtc!=null).Select(x=>x.CampaignId).ToListAsync(ct);
        var fids = fundingRows.Select(x => x.Id).ToArray(); var allocations = await (from a in db.CampaignFundingAllocations.AsNoTracking() join p in db.Participations.AsNoTracking() on a.ParticipationId equals p.Id join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id where fids.Contains(a.FundingId) select new { a, k.DisplayName }).ToListAsync(ct); var campaignIds = rows.Select(x => x.c.Id).ToArray(); var candidates = await (from p in db.Participations.AsNoTracking() join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id where campaignIds.Contains(p.CampaignId) && (p.Status == "Confirmed" || p.Status == "Completed" || p.Status=="Invited"&&db.Campaigns.Any(c=>c.Id==p.CampaignId&&c.CheckoutAtCreation)) select new { p.CampaignId, p.Id, p.Status,k.DisplayName }).ToListAsync(ct);
        return rows.Select(x =>
        {
            var campaignFundings = fundingRows.Where(f => f.CampaignId == x.c.Id).OrderByDescending(f => f.VersionNo).ToList();
            var latest = campaignFundings.FirstOrDefault();
            var currentDraft = latest?.Status == CampaignFundingStatuses.Draft ? latest : null;
            var priorFundings = campaignFundings.Where(f => f.Status != CampaignFundingStatuses.Cancelled && f.Id != currentDraft?.Id).ToList();
            var priorFundingIds = priorFundings.Select(f => f.Id).ToHashSet();
            var previouslyAllocatedIds = allocations.Where(a => priorFundingIds.Contains(a.a.FundingId)).Select(a => a.a.ParticipationId).ToHashSet();
            var available = candidates.Where(a => a.CampaignId == x.c.Id && !previouslyAllocatedIds.Contains(a.Id)).ToList();
            var prepaidFunding = campaignFundings.FirstOrDefault(f => f.AllocationMode == CampaignAllocationModes.Prepaid
                && f.Status is (CampaignFundingStatuses.Paid or CampaignFundingStatuses.PartiallyReleased or CampaignFundingStatuses.Released)
                && allocations.Where(a => a.a.FundingId == f.Id).Sum(a => a.a.Amount) < f.CampaignBudgetSnapshot);
            var isAllocatingPrepaid = currentDraft is null && prepaidFunding is not null && available.Count > 0;
            var activeFunding = currentDraft ?? (isAllocatingPrepaid ? prepaidFunding : available.Count == 0 ? latest : null);
            var activeAllocations = allocations.Where(a => activeFunding is not null && a.a.FundingId == activeFunding.Id).ToList();
            var batchCandidates = isAllocatingPrepaid ? available : activeFunding is not null && activeFunding.Status != CampaignFundingStatuses.Draft
                ? candidates.Where(a => activeAllocations.Any(old => old.a.ParticipationId == a.Id)).ToList()
                : available;
            var committedBudget = campaignFundings.Where(f => f.Status != CampaignFundingStatuses.Cancelled && f.Id != currentDraft?.Id).Sum(f => f.CampaignBudgetSnapshot);
            var prepaidAvailable = prepaidFunding is null ? 0 : prepaidFunding.CampaignBudgetSnapshot-allocations.Where(a=>a.a.FundingId==prepaidFunding.Id).Sum(a=>a.a.Amount);
            var batchBudget = isAllocatingPrepaid ? prepaidAvailable : activeFunding?.CampaignBudgetSnapshot ?? 0m;
            var itemAllocations = activeFunding is not null && activeFunding.Status != CampaignFundingStatuses.Draft && !isAllocatingPrepaid
                ? activeAllocations.Select(a => new CampaignFundingAllocationItem(a.a.Id,a.a.ParticipationId,a.DisplayName,a.a.Amount,a.a.AcceptedAmount,a.a.RefundedAmount,a.a.Status,a.a.ReleasedAtUtc)).ToList()
                : batchCandidates.Select(a => { var old=activeAllocations.FirstOrDefault(v=>v.a.ParticipationId==a.Id); return new CampaignFundingAllocationItem(old?.a.Id??Guid.Empty,a.Id,a.DisplayName,old?.a.Amount??0,old?.a.AcceptedAmount??0,old?.a.RefundedAmount??0,old?.a.Status??"Active",old?.a.ReleasedAtUtc); }).ToList();
            var status = activeFunding?.Status ?? CampaignFundingStatuses.Draft;
            return new CampaignFundingItem(activeFunding?.Id,x.c.Id,activeFunding?.VersionNo??((latest?.VersionNo??0)+1),x.c.Title,x.b.CompanyName,batchBudget,activeFunding?.AllocationMode??CampaignAllocationModes.Custom,status,activeFunding?.OrderNumber,activeFunding?.PaymentMethod??CampaignPaymentMethods.BankTransfer,activeFunding?.FeeRateSnapshot??0,activeFunding?.FixedFeeSnapshot??0,activeFunding?.PlatformFeeSnapshot??0,activeFunding?.GatewayFeeRateSnapshot??0,activeFunding?.GatewayFeeSnapshot??0,activeFunding?.GatewayFeeBearerSnapshot??"Brand",activeFunding?.TotalPayableSnapshot??0,activeFunding?.DisputeHoldDaysSnapshot??0,activeFunding?.PaymentDueDaysSnapshot??0,activeFunding?.RefundPolicySnapshot??"",activeFunding?.SubmittedAtUtc,activeFunding?.DueAtUtc,activeFunding?.PaidAtUtc,activeFunding?.PaymentReference,activeFunding?.PaymentProofUrl,activeFunding?.PaymentProofNote,activeFunding?.PaymentProofSubmittedAtUtc,activeFunding?.CancelledAtUtc,activeFunding?.CancellationReason,activeFunding?.RefundedAmount??0,activeFunding?.RefundReference,activeFunding?.RefundBankName,activeFunding?.RefundAccountNumber,activeFunding?.RefundAccountHolder,itemAllocations)
            { VatAmount=activeFunding?.VatAmountSnapshot??0,VatAdded=activeFunding?.VatAddedSnapshot??0,VatMode=activeFunding?.VatModeSnapshot??"None",VatRate=activeFunding?.VatRateSnapshot??0,BalanceAppliedAmount=activeFunding?.BalanceAppliedAmount??0, OnlinePaymentStartedAtUtc=activeFunding?.OnlinePaymentStartedAtUtc, ConfirmedKocCount=candidates.Count(a=>a.CampaignId==x.c.Id&&(a.Status=="Confirmed"||a.Status=="Completed")),DesiredKocCount=x.c.DesiredKocCount,IsShortlistFinalized=finalizedCampaigns.Contains(x.c.Id),CampaignStatus=x.c.Status,SavedAllocationsMatch=activeFunding is not null&&activeFunding.Status==CampaignFundingStatuses.Draft&&activeAllocations.Count==batchCandidates.Count&&activeFunding.CampaignBudgetSnapshot==batchBudget&&activeAllocations.Select(a=>a.a.ParticipationId).ToHashSet().SetEquals(batchCandidates.Select(a=>a.Id)),TotalCampaignBudget=x.c.BudgetAmount,PreviouslyAllocatedBudget=committedBudget,IsAllocatingPrepaidReserve=isAllocatingPrepaid };
        }).ToList();
    }
    public async Task<CampaignFundingResult> PrepareFullFundingAsync(Guid userId, Guid campaignId, CancellationToken ct = default, decimal? amount = null)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var campaign=await(from c in db.Campaigns join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id==campaignId&&b.UserId==userId select c).FirstOrDefaultAsync(ct);
        if(campaign is null)return new(false,"Không tìm thấy chiến dịch.");
        if(!(campaign.CheckoutAtCreation&&campaign.Status=="Draft") && campaign.Status is not (Topstar.Application.Campaigns.CampaignStatuses.Published or Topstar.Application.Campaigns.CampaignStatuses.InProgress))return new(false,"Chỉ có thể ký quỹ cho chiến dịch đã được duyệt và chưa kết thúc.");
        var rows=await db.CampaignFundings.Where(x=>x.CampaignId==campaignId).OrderByDescending(x=>x.VersionNo).ToListAsync(ct);
        if(rows.Any(x=>x.Status is CampaignFundingStatuses.Draft or CampaignFundingStatuses.Pending))return new(false,"Chiến dịch đang có một đợt ký quỹ chưa hoàn tất.");
        var committed=rows.Where(x=>x.Status!=CampaignFundingStatuses.Cancelled).Sum(x=>x.CampaignBudgetSnapshot);
        var remaining=campaign.BudgetAmount-committed;if(remaining<=0)return new(false,"Toàn bộ ngân sách chiến dịch đã được ký quỹ.");
        var requestedAmount = amount ?? remaining;
        if (!CampaignFundingRules.IsValidPrepayment(requestedAmount, remaining))
            return new(false, "Số tiền ký quỹ phải là số nguyên đồng, lớn hơn 0 và không vượt ngân sách còn lại.");
        if(campaign.CheckoutAtCreation&&campaign.Status=="Draft"&&requestedAmount!=campaign.BudgetAmount)return new(false,"Cần thanh toán toàn bộ ngân sách đã xác nhận khi tạo chiến dịch.");
        var now=DateTime.UtcNow;var funding=new CampaignFundingEntity{Id=Guid.NewGuid(),CampaignId=campaignId,VersionNo=(rows.FirstOrDefault()?.VersionNo??0)+1,AllocationMode=CampaignAllocationModes.Prepaid,Status=CampaignFundingStatuses.Draft,CampaignBudgetSnapshot=requestedAmount,TotalPayableSnapshot=requestedAmount,CreatedAtUtc=now,UpdatedAtUtc=now,UpdatedByUserId=userId};
        db.CampaignFundings.Add(funding);Audit(userId,funding.Id,"PrepareFullFunding",new{Amount=requestedAmount},now);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("CK_CampaignFundings_Mode", StringComparison.OrdinalIgnoreCase) == true)
        {
            await tx.RollbackAsync(ct);
            return new(false, "Hệ thống chưa cập nhật cấu hình ký quỹ toàn bộ. Vui lòng liên hệ quản trị để cập nhật trước khi thử lại.");
        }
        return new(true,$"Đã chuẩn bị yêu cầu ký quỹ trước {requestedAmount:N0} ₫.",funding.Id);
    }

    public async Task<CampaignFundingResult> SaveAllocationsAsync(Guid userId, Guid campaignId, string mode, IReadOnlyDictionary<Guid, decimal> custom, CancellationToken ct = default)
    {
        if (mode is not (CampaignAllocationModes.Equal or CampaignAllocationModes.Custom)) return new(false, "Phương thức phân bổ không hợp lệ."); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var row = await (from c in db.Campaigns join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id == campaignId && b.UserId == userId select c).FirstOrDefaultAsync(ct); if (row is null) return new(false, "Không tìm thấy chiến dịch.");
        var allParts = await db.Participations.Where(x => x.CampaignId == campaignId && (x.Status == "Confirmed" || x.Status == "Completed" || row.CheckoutAtCreation&&x.Status=="Invited")).OrderBy(x => x.ConfirmedAtUtc).ToListAsync(ct); var eligibility=CampaignFundingRules.ValidateEligibility(row.Status,row.BudgetAmount,false,row.DesiredKocCount,allParts.Count); if(eligibility is not null)return new(false,eligibility);
        var fundings = await db.CampaignFundings.Where(x=>x.CampaignId==campaignId).OrderByDescending(x=>x.VersionNo).ToListAsync(ct); var latest=fundings.FirstOrDefault(); if(latest?.Status==CampaignFundingStatuses.Pending)return new(false,"Đợt ký quỹ hiện tại đang chờ thanh toán. Vui lòng hoàn tất hoặc hủy trước khi lập đợt mới.");
        var funding=latest?.Status==CampaignFundingStatuses.Draft?latest:null; var priorIds=fundings.Where(x=>x.Status!=CampaignFundingStatuses.Cancelled&&x.Id!=funding?.Id).Select(x=>x.Id).ToArray(); var priorAllocations=await db.CampaignFundingAllocations.AsNoTracking().Where(x=>priorIds.Contains(x.FundingId)).ToListAsync(ct); var usedIds=priorAllocations.Select(x=>x.ParticipationId).ToHashSet(); var parts=allParts.Where(x=>!usedIds.Contains(x.Id)).ToList(); if(parts.Count==0)return new(false,"Chưa có KOC mới cần phân bổ trong đợt tiếp theo.");
        var previouslyAllocated=priorAllocations.Sum(x=>x.Amount);var remaining=Math.Max(0,row.BudgetAmount-previouslyAllocated);if(remaining<=0)return new(false,"Ngân sách chiến dịch đã được phân bổ hết.");
        var prepaid=funding is null?fundings.FirstOrDefault(x=>x.AllocationMode==CampaignAllocationModes.Prepaid&&x.Status is (CampaignFundingStatuses.Paid or CampaignFundingStatuses.PartiallyReleased or CampaignFundingStatuses.Released)):null;
        if(prepaid is not null)
        {
            var allocated=await db.CampaignFundingAllocations.Where(x=>x.FundingId==prepaid.Id).SumAsync(x=>x.Amount,ct);var capacity=prepaid.CampaignBudgetSnapshot-allocated;
            var reserveAmount=mode==CampaignAllocationModes.Custom?custom.Values.Sum():capacity;var reserveValues=mode==CampaignAllocationModes.Equal?CampaignFundingRules.SplitEqual(reserveAmount,parts.Select(x=>x.Id).ToList()):custom;
            var reserveError=CampaignFundingRules.ValidateAllocations(reserveAmount,reserveValues,parts.Select(x=>x.Id).ToHashSet());if(reserveError is not null)return new(false,reserveError);if(reserveAmount>capacity)return new(false,$"Tổng thù lao vượt quá số dư ký quỹ còn lại {capacity:N0} ₫.");
            var reserveNow=DateTime.UtcNow;foreach(var p in parts){
              db.CampaignFundingAllocations.Add(new(){Id=Guid.NewGuid(),FundingId=prepaid.Id,ParticipationId=p.Id,Amount=reserveValues[p.Id],Status="Active",CreatedAtUtc=reserveNow});
              if(row.CheckoutAtCreation&&p.Status=="Invited"){
                var kocUser=await db.KocProfiles.Where(k=>k.Id==p.KocProfileId).Select(k=>k.UserId).SingleAsync(ct);
                db.Notifications.Add(NotificationFactory.Create(kocUser,"Participation","Brand đã chốt thù lao",$"Thù lao trước khấu trừ {reserveValues[p.Id]:N0} đ cho chiến dịch {row.Title}. Xem phí, thuế và xác nhận nhận việc.",reserveNow,"Participation",p.Id,audienceRole:"Koc"));
              }
            }if(prepaid.Status==CampaignFundingStatuses.Released)prepaid.Status=CampaignFundingStatuses.Paid;prepaid.UpdatedAtUtc=reserveNow;prepaid.UpdatedByUserId=userId;Audit(userId,prepaid.Id,"AllocatePrepaidReserve",new{Amount=reserveAmount,Remaining=capacity-reserveAmount,Count=parts.Count},reserveNow);
            try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}catch(DbUpdateException){await tx.RollbackAsync(ct);return new(false,"Không thể phân bổ từ số dư ký quỹ. Vui lòng tải lại và thử lại.");}return new(true,"Đã phân bổ thù lao từ số tiền Brand đã ký quỹ; không cần thanh toán thêm.",prepaid.Id);
        }
        var batchBudget=mode==CampaignAllocationModes.Custom?custom.Values.Sum():(parts.Count+usedIds.Count>=row.DesiredKocCount?remaining:Math.Min(remaining,decimal.Round(row.BudgetAmount*parts.Count/row.DesiredKocCount,0,MidpointRounding.AwayFromZero)));
        if(batchBudget<=0||batchBudget>remaining)return new(false,$"Tổng thù lao đợt này phải lớn hơn 0 và không vượt quá ngân sách còn lại {remaining:N0} ₫.");
        var values = mode == CampaignAllocationModes.Equal ? CampaignFundingRules.SplitEqual(batchBudget, parts.Select(x => x.Id).ToList()) : custom; var error = CampaignFundingRules.ValidateAllocations(batchBudget, values, parts.Select(x => x.Id).ToHashSet()); if (error is not null) return new(false, error);
        var now = DateTime.UtcNow; if(funding is null){funding=new(){Id=Guid.NewGuid(),CampaignId=campaignId,VersionNo=(latest?.VersionNo??0)+1,Status=CampaignFundingStatuses.Draft,CreatedAtUtc=now};db.CampaignFundings.Add(funding);}
        funding.CampaignBudgetSnapshot=batchBudget; funding.TotalPayableSnapshot=batchBudget; funding.AllocationMode = mode; funding.UpdatedAtUtc = now; funding.UpdatedByUserId = userId; var old = await db.CampaignFundingAllocations.Where(x => x.FundingId == funding.Id).ToListAsync(ct); db.CampaignFundingAllocations.RemoveRange(old); foreach (var p in parts) db.CampaignFundingAllocations.Add(new() { Id = Guid.NewGuid(), FundingId = funding.Id, ParticipationId = p.Id, Amount = values[p.Id], Status = "Active", CreatedAtUtc = now }); Audit(userId, funding.Id, "SaveAllocationBatch", new { mode, BatchBudget=batchBudget, CampaignBudget=row.BudgetAmount, PreviouslyAllocated=previouslyAllocated, Count = parts.Count, funding.VersionNo }, now);
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (DbUpdateException) { await tx.RollbackAsync(ct); return new(false, "Không thể lưu bảng phân bổ vì dữ liệu quỹ vừa thay đổi hoặc chưa hợp lệ. Vui lòng tải lại và thử lại."); }
        return new(true, "Đã lưu bảng phân bổ thù lao.", funding.Id);
    }
    private async Task<CampaignFundingResult> SubmitLegacyAsync(Guid userId, Guid campaignId, string paymentMethod, CancellationToken ct = default)
    {
        if (paymentMethod is not (CampaignPaymentMethods.BankTransfer or CampaignPaymentMethods.OnePay)) return new(false, "Phương thức thanh toán không hợp lệ."); var paymentConfig = await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct); if (paymentConfig is null || (paymentMethod == CampaignPaymentMethods.BankTransfer && !paymentConfig.BankTransferEnabled) || (paymentMethod == CampaignPaymentMethods.OnePay && !paymentConfig.OnePayEnabled)) return new(false, "Phương thức thanh toán đang tạm khóa.");
        var funding = await (from f in db.CampaignFundings join c in db.Campaigns on f.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where f.CampaignId == campaignId && b.UserId == userId select f).FirstOrDefaultAsync(ct); if (funding is null || funding.Status != CampaignFundingStatuses.Draft) return new(false, "Bảng phân bổ không tồn tại hoặc đã được chốt."); var sum = await db.CampaignFundingAllocations.Where(x => x.FundingId == funding.Id).SumAsync(x => x.Amount, ct); if (sum != funding.CampaignBudgetSnapshot) return new(false, "Tổng phân bổ chưa bằng ngân sách chiến dịch."); var now = DateTime.UtcNow; var p = await db.CampaignFeePolicies.AsNoTracking().Where(x => x.Code == "DEFAULT" && x.IsActive && x.EffectiveAtUtc <= now).OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.EffectiveAtUtc).FirstAsync(ct); var fee = BillingRules.CalculateFee(sum, p.FeeRate, p.FixedFee, p.MinimumFee, p.MaximumFee); var gateway = paymentMethod == CampaignPaymentMethods.OnePay && p.GatewayFeeBearer == "Brand" ? decimal.Round((sum + fee) * p.GatewayFeeRate, 0, MidpointRounding.AwayFromZero) : 0; funding.Status = CampaignFundingStatuses.Pending; funding.PaymentMethod = paymentMethod; funding.OrderNumber = $"CF-{now:yyyyMMdd}-{campaignId.ToString("N")[..8].ToUpperInvariant()}"; funding.FeeRateSnapshot = p.FeeRate; funding.FixedFeeSnapshot = p.FixedFee; funding.PlatformFeeSnapshot = fee; funding.GatewayFeeRateSnapshot = p.GatewayFeeRate; funding.GatewayFeeSnapshot = gateway; funding.GatewayFeeBearerSnapshot = p.GatewayFeeBearer; var vatQuote=await BrandTaxPricing.QuoteAsync(db,"Funding",sum+fee,fee,ct); funding.VatPolicyIdSnapshot=vatQuote.PolicyId;funding.VatModeSnapshot=vatQuote.Mode;funding.VatRateSnapshot=vatQuote.Rate;funding.VatAmountSnapshot=vatQuote.Vat;funding.VatAddedSnapshot=vatQuote.AddedVat;funding.TotalPayableSnapshot = vatQuote.Total + gateway; funding.DisputeHoldDaysSnapshot = p.DisputeHoldDays; funding.PaymentDueDaysSnapshot = p.PaymentDueDays; funding.RefundPolicySnapshot = p.RefundPolicy; funding.SubmittedAtUtc = now; funding.DueAtUtc = now.AddDays(p.PaymentDueDays); funding.UpdatedAtUtc = now; funding.UpdatedByUserId = userId; db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = funding.Id, EntryType = "FundingRequested", Amount = funding.TotalPayableSnapshot, Note = funding.OrderNumber, ActorUserId = userId, CreatedAtUtc = now }); Audit(userId, funding.Id, "SubmitFunding", new { funding.PaymentMethod, funding.TotalPayableSnapshot, fee, gateway }, now); await db.SaveChangesAsync(ct); return new(true, "Đã tạo yêu cầu thanh toán chiến dịch.", funding.Id);
    }
    public async Task<CampaignFundingResult> SubmitAsync(Guid userId, Guid campaignId, string paymentMethod, CancellationToken ct = default,decimal? expectedTotal=null)
    {
        var refundAccount=await db.BrandProfiles.AsNoTracking().Where(x=>x.UserId==userId).Select(x=>new{x.RefundBankName,x.RefundAccountNumber,x.RefundAccountHolder}).FirstOrDefaultAsync(ct);
        var refundBankName=(refundAccount?.RefundBankName??string.Empty).Trim();var refundAccountNumber=(refundAccount?.RefundAccountNumber??string.Empty).Trim();var refundAccountHolder=(refundAccount?.RefundAccountHolder??string.Empty).Trim();
        if(refundBankName.Length >200)return new(false,"Tên ngân hàng nhận hoàn phải từ 2 đến 200 ký tự.");
        if(refundAccountNumber.Length >100||refundAccountNumber.Any(x=>!char.IsLetterOrDigit(x)&&x!='-'&&x!=' '))return new(false,"Số tài khoản nhận hoàn không hợp lệ.");
        if(refundAccountHolder.Length >200)return new(false,"Tên chủ tài khoản nhận hoàn phải từ 2 đến 200 ký tự.");
        if (paymentMethod is not (CampaignPaymentMethods.BankTransfer or CampaignPaymentMethods.OnePay or CampaignPaymentMethods.VnPay or "BrandBalance")) return new(false, "Phương thức thanh toán không hợp lệ.");
        var paymentConfig = await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct);
        if (paymentConfig is null || paymentMethod == CampaignPaymentMethods.BankTransfer && !paymentConfig.BankTransferEnabled || paymentMethod == CampaignPaymentMethods.OnePay && !paymentConfig.OnePayEnabled || paymentMethod == CampaignPaymentMethods.VnPay && !paymentConfig.VnPayEnabled) return new(false, "Phương thức thanh toán đang tạm khóa.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var funding = await (from f in db.CampaignFundings join c in db.Campaigns on f.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where f.CampaignId == campaignId && b.UserId == userId orderby f.VersionNo descending select f).FirstOrDefaultAsync(ct);
        if (funding is null || funding.Status != CampaignFundingStatuses.Draft) return new(false, "Bảng phân bổ mới nhất không tồn tại hoặc đã được chốt.");
        var campaign=await db.Campaigns.AsNoTracking().FirstAsync(x=>x.Id==campaignId,ct);
        var confirmedCount=await db.Participations.AsNoTracking().CountAsync(x=>x.CampaignId==campaignId&&(x.Status=="Confirmed"||x.Status=="Completed"),ct);
        var eligibility=funding.AllocationMode==CampaignAllocationModes.Prepaid
            ? ((campaign.CheckoutAtCreation&&campaign.Status=="Draft")||campaign.Status is Topstar.Application.Campaigns.CampaignStatuses.Published or Topstar.Application.Campaigns.CampaignStatuses.InProgress ? null : "Chiến dịch không ở trạng thái có thể ký quỹ.")
            : CampaignFundingRules.ValidateEligibility(campaign.Status,campaign.BudgetAmount,false,campaign.DesiredKocCount,confirmedCount);
        if(eligibility is not null)return new(false,eligibility);
        var allocations=await db.CampaignFundingAllocations.AsNoTracking().Where(x=>x.FundingId==funding.Id).ToListAsync(ct);
        var expected=allocations.Select(x=>x.ParticipationId).Distinct().ToList();
        if(allocations.Select(x=>x.ParticipationId).Distinct().Count()!=allocations.Count)return new(false,"Bảng phân bổ có KOC trùng lặp. Vui lòng kiểm tra và lưu lại.");
        var committed=await db.CampaignFundings.AsNoTracking().Where(f=>f.CampaignId==campaignId&&f.Id!=funding.Id&&f.Status!=CampaignFundingStatuses.Cancelled).SumAsync(f=>f.CampaignBudgetSnapshot,ct);
        if(committed+funding.CampaignBudgetSnapshot>campaign.BudgetAmount)return new(false,"Tổng các đợt phân bổ vượt ngân sách chiến dịch.");
        var validation=funding.AllocationMode==CampaignAllocationModes.Prepaid
            ? (funding.CampaignBudgetSnapshot<=0||funding.CampaignBudgetSnapshot!=decimal.Truncate(funding.CampaignBudgetSnapshot)?"Số tiền ký quỹ phải là số nguyên đồng lớn hơn 0.":null)
            : CampaignFundingRules.ValidateAllocations(funding.CampaignBudgetSnapshot,allocations.ToDictionary(x=>x.ParticipationId,x=>x.Amount),expected);
        if(validation is not null)return new(false,validation);
        var sum=funding.AllocationMode==CampaignAllocationModes.Prepaid?funding.CampaignBudgetSnapshot:allocations.Sum(x=>x.Amount);
        if(campaign.CheckoutAtCreation&&campaign.Status=="Draft"&&sum!=campaign.BudgetAmount)return new(false,"Cần thanh toán toàn bộ ngân sách khi tạo chiến dịch.");
        var now = DateTime.UtcNow;
        var policy = await db.CampaignFeePolicies.AsNoTracking().Where(x => x.Code == "DEFAULT" && x.IsActive && x.EffectiveAtUtc <= now).OrderByDescending(x => x.EffectiveAtUtc).FirstOrDefaultAsync(ct);
        if (policy is null) return new(false, "Chưa có chính sách phí đang hiệu lực.");
        var fee = BillingRules.CalculateFee(sum, policy.FeeRate, policy.FixedFee, policy.MinimumFee, policy.MaximumFee);
        var gateway = paymentMethod != CampaignPaymentMethods.BankTransfer && policy.GatewayFeeBearer == "Brand" ? decimal.Round((sum + fee) * policy.GatewayFeeRate, 0, MidpointRounding.AwayFromZero) : 0;
        var taxPreview=await BrandTaxPricing.QuoteAsync(db,"Funding",sum+fee,fee,ct);
        if(expectedTotal.HasValue && expectedTotal.Value!=taxPreview.Total+gateway)return new(false,"Chi phí ký quỹ đã thay đổi. Vui lòng kiểm tra lại phí và tổng tiền trước khi xác nhận.");
        funding.Status = CampaignFundingStatuses.Pending; funding.PaymentMethod = paymentMethod; funding.OrderNumber = $"CF-{now:yyyyMMdd}-{campaignId.ToString("N")[..8].ToUpperInvariant()}-V{funding.VersionNo}";funding.RefundBankName=refundBankName;funding.RefundAccountNumber=refundAccountNumber;funding.RefundAccountHolder=refundAccountHolder;
        funding.FeeRateSnapshot = policy.FeeRate; funding.FixedFeeSnapshot = policy.FixedFee; funding.PlatformFeeSnapshot = fee; funding.GatewayFeeRateSnapshot = policy.GatewayFeeRate; funding.GatewayFeeSnapshot = gateway; funding.GatewayFeeBearerSnapshot = policy.GatewayFeeBearer; var vatQuote=taxPreview; funding.VatPolicyIdSnapshot=vatQuote.PolicyId;funding.VatModeSnapshot=vatQuote.Mode;funding.VatRateSnapshot=vatQuote.Rate;funding.VatAmountSnapshot=vatQuote.Vat;funding.VatAddedSnapshot=vatQuote.AddedVat;funding.TotalPayableSnapshot = vatQuote.Total + gateway; funding.DisputeHoldDaysSnapshot = policy.DisputeHoldDays; funding.PaymentDueDaysSnapshot = policy.PaymentDueDays; funding.RefundPolicySnapshot = policy.RefundPolicy; funding.SubmittedAtUtc = now; funding.DueAtUtc = now.AddDays(policy.PaymentDueDays); funding.UpdatedAtUtc = now; funding.UpdatedByUserId = userId;
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = funding.Id, EntryType = "FundingRequested", Amount = funding.TotalPayableSnapshot, Note = funding.OrderNumber, ActorUserId = userId, CreatedAtUtc = now });
        Audit(userId, funding.Id, "SubmitFunding", new { funding.VersionNo, funding.PaymentMethod, funding.TotalPayableSnapshot, fee, gateway,refundBankName,refundAccountNumber,refundAccountHolder }, now);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            return new(false, "Không thể tạo yêu cầu thanh toán do dữ liệu quỹ vừa thay đổi hoặc chưa hợp lệ. Vui lòng tải lại và kiểm tra bảng phân bổ.");
        }
        return new(true, paymentMethod != CampaignPaymentMethods.BankTransfer
            ? "Đã khóa bảng phân bổ. Vui lòng tiếp tục thanh toán online."
            : "Đã tạo yêu cầu chuyển khoản. Vui lòng chuyển đúng tổng tiền và nội dung thanh toán hiển thị bên dưới.", funding.Id);
    }

    public async Task<CampaignFundingResult> ConfirmPaidAsync(Guid actor, Guid id, string reference, string? manualReason = null, CancellationToken ct = default)
    {
        await using var paymentTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        reference = reference?.Trim() ?? string.Empty;
        manualReason = manualReason?.Trim();
        if (reference.Length is < 3 or > 200) return new(false, "Mã tham chiếu phải từ 3 đến 200 ký tự.");
        var f = await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
        if (f is null || f.Status != CampaignFundingStatuses.Pending) return new(false, "Yêu cầu thanh toán không ở trạng thái chờ.");
        if (f.PaymentMethod is CampaignPaymentMethods.OnePay or CampaignPaymentMethods.VnPay or "BrandBalance") return new(false, "Thanh toán online chỉ được xác nhận bằng callback/IPN có chữ ký hợp lệ.");
        var latestBrandEvidenceId = await db.CampaignFundingEvidences.AsNoTracking().Where(x => x.FundingId == id && x.EvidenceType == CampaignFundingEvidenceTypes.BrandPayment).OrderByDescending(x => x.CreatedAtUtc).Select(x => x.Id).FirstOrDefaultAsync(ct);
        var latestReviewStatus = latestBrandEvidenceId == Guid.Empty ? null : await db.CampaignFundingEvidenceReviews.AsNoTracking().Where(x => x.EvidenceId == latestBrandEvidenceId).OrderByDescending(x => x.CreatedAtUtc).Select(x => x.Status).FirstOrDefaultAsync(ct);
        var reconciliation = await db.CampaignFundingEvidences.AsNoTracking()
            .Where(x => x.FundingId == id && x.EvidenceType == CampaignFundingEvidenceTypes.Reconciliation && x.Reference == reference)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (reconciliation?.Amount is decimal reconciledAmount && reconciledAmount < (f.TotalPayableSnapshot-f.BalanceAppliedAmount)) return new(false, $"Số tiền thực nhận còn thiếu {((f.TotalPayableSnapshot-f.BalanceAppliedAmount) - reconciledAmount):N0} ₫. Chưa thể xác nhận đủ tiền.");
        if (reconciliation?.Amount is decimal excessAmount && excessAmount > (f.TotalPayableSnapshot-f.BalanceAppliedAmount)) return new(false, $"Số tiền thực nhận thừa {(excessAmount - (f.TotalPayableSnapshot-f.BalanceAppliedAmount)):N0} ₫. Cần xử lý chênh lệch trước khi xác nhận.");
        var hasStandardEvidence = !string.IsNullOrWhiteSpace(f.PaymentProofUrl)
            && f.PaymentProofSubmittedAtUtc.HasValue
            && latestReviewStatus == CampaignFundingEvidenceReviewStatuses.Accepted
            && reconciliation?.Amount == (f.TotalPayableSnapshot-f.BalanceAppliedAmount);
        if (!hasStandardEvidence && (string.IsNullOrWhiteSpace(manualReason) || manualReason.Length is < 10 or > 500))
            return new(false, "Khi chưa đủ chứng từ, Admin phải nhập lý do xác nhận thủ công từ 10 đến 500 ký tự.");
        var confirmationAction = hasStandardEvidence ? "ConfirmPaid" : "ConfirmPaidManual";
        var approval = await RequireSecondApproverAsync(actor, f.Id, confirmationAction, f.TotalPayableSnapshot, reference, ct);
        if (approval is not null){await paymentTx.CommitAsync(ct);return approval;}
        var now = DateTime.UtcNow;
        f.Status = CampaignFundingStatuses.Paid; f.PaidAtUtc = now; f.PaymentReference = reference; f.UpdatedAtUtc = now; f.UpdatedByUserId = actor;
        var campaignInfo = await (from campaign in db.Campaigns.AsNoTracking()
                                  join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                  where campaign.Id == f.CampaignId
                                  select new { campaign.Title,campaign.CheckoutAtCreation, brand.UserId }).FirstAsync(ct);
        db.Notifications.Add(NotificationFactory.Create(campaignInfo.UserId, "CampaignFunding", "Ký quỹ đã được xác nhận", campaignInfo.CheckoutAtCreation ? $"Đã ghi nhận thanh toán chiến dịch {campaignInfo.Title}. Hồ sơ đang chờ duyệt nội dung." : $"Khoản ký quỹ cho chiến dịch {campaignInfo.Title} đã được xác nhận. Bạn có thể mở công việc cho KOC đã được phân bổ thù lao.", now, "Campaign", f.CampaignId, audienceRole: "Brand"));
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = f.Id, EntryType = "Paid", Amount = f.TotalPayableSnapshot-f.BalanceAppliedAmount, Note = hasStandardEvidence ? reference : $"{reference} - Manual confirmation", ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, f.Id, confirmationAction, new { reference, f.TotalPayableSnapshot, ManualReason = hasStandardEvidence ? null : manualReason }, now);
        await db.SaveChangesAsync(ct);
        await paymentTx.CommitAsync(ct);
        return new(true, "Đã xác nhận đủ tiền ký quỹ. Brand có thể chủ động mở công việc cho KOC đủ điều kiện.", f.Id);
    }

    public async Task<CampaignFundingResult> SubmitPaymentProofAsync(Guid actor, Guid fundingId, string proofUrl, string note, CancellationToken ct = default)
    {
        await using var proofTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        note = (note ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(proofUrl) || !proofUrl.StartsWith("/uploads/payment-proofs/", StringComparison.OrdinalIgnoreCase)) return new(false, "Ảnh bằng chứng thanh toán không hợp lệ.");
        if (note.Length < 10 || note.Length > 500) return new(false, "Ghi chú chuyển khoản phải từ 10 đến 500 ký tự.");
        var funding = await (from f in db.CampaignFundings join c in db.Campaigns on f.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where f.Id == fundingId && b.UserId == actor select f).FirstOrDefaultAsync(ct);
        if (funding is null) return new(false, "Không tìm thấy yêu cầu thanh toán hoặc Brand không có quyền thao tác.");
        if (funding.Status != CampaignFundingStatuses.Pending || funding.PaymentMethod != CampaignPaymentMethods.BankTransfer) return new(false, "Chỉ được gửi bằng chứng cho yêu cầu chuyển khoản đang chờ thanh toán.");
        var now = DateTime.UtcNow;
        funding.PaymentProofUrl = proofUrl; funding.PaymentProofNote = note; funding.PaymentProofSubmittedAtUtc = now; funding.UpdatedAtUtc = now; funding.UpdatedByUserId = actor;
        db.CampaignFundingEvidences.Add(new() { Id = Guid.NewGuid(), FundingId = funding.Id, EvidenceType = CampaignFundingEvidenceTypes.BrandPayment, FileUrl = proofUrl, Note = note, ActorUserId = actor, CreatedAtUtc = now });
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = funding.Id, EntryType = "PaymentProof", Amount = 0, Note = "Brand submitted bank-transfer proof", ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, funding.Id, "SubmitPaymentProof", new { HasProof = true, NoteLength = note.Length }, now);
        try { await db.SaveChangesAsync(ct); await proofTx.CommitAsync(ct); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not save payment proof for campaign funding {FundingId}", fundingId);
            return new(false, "Không thể lưu bằng chứng thanh toán. Vui lòng tải lại và thử lại.");
        }
        return new(true, "Đã gửi bằng chứng chuyển khoản. Topstar sẽ kiểm tra và đối soát.", funding.Id);
    }
    public async Task<CampaignFundingResult> CancelPendingAsync(Guid actor, Guid fundingId, string reason, CancellationToken ct = default)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length < 10 || reason.Length > 500) return new(false, "Lý do hủy phải từ 10 đến 500 ký tự.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = await (from f in db.CampaignFundings join c in db.Campaigns on f.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where f.Id == fundingId && b.UserId == actor select f).FirstOrDefaultAsync(ct);
        if (row is null) return new(false, "Không tìm thấy yêu cầu hoặc Brand không có quyền thao tác.");
        if (row.Status is not (CampaignFundingStatuses.Draft or CampaignFundingStatuses.Pending))
            return new(false, "Chỉ được hủy bản nháp hoặc yêu cầu đang chờ thanh toán. Khoản đã thanh toán cần được đối soát hoàn tiền.");
        if(row.PaymentMethod==CampaignPaymentMethods.OnePay||row.OnlinePaymentStartedAtUtc.HasValue||row.PaymentProofSubmittedAtUtc.HasValue||await db.CampaignFundingEvidences.AnyAsync(e=>e.FundingId==row.Id&&e.EvidenceType==CampaignFundingEvidenceTypes.Reconciliation,ct))
            return new(false,"Yêu cầu đã bắt đầu thanh toán hoặc đang đối soát. Vui lòng liên hệ hỗ trợ để xác minh giao dịch trước khi hủy.");
        var now = DateTime.UtcNow;
        row.Status = CampaignFundingStatuses.Cancelled; row.CancelledAtUtc = now; row.CancellationReason = reason; row.UpdatedAtUtc = now; row.UpdatedByUserId = actor;
        if(row.BalanceAppliedAmount>0)
        {
            var brandId=await db.BrandProfiles.Where(x=>x.UserId==actor).Select(x=>x.Id).SingleAsync(ct);
            var account=await BrandBalanceService.LockAsync(db,brandId,ct);
            await BrandBalanceService.WriteAsync(db,account,row.BalanceAppliedAmount,0,"FundingCancelled",row.Id,Guid.NewGuid(),"Trả lại phần số dư đã dùng cho yêu cầu ký quỹ bị hủy.",actor,ct);
            db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=row.Id,EntryType="BalancePaymentReversed",Amount=-row.BalanceAppliedAmount,Note="Hoàn phần số dư đã dùng do hủy yêu cầu chưa thanh toán.",ActorUserId=actor,CreatedAtUtc=now});
        }
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = row.Id, EntryType = "Cancelled", Amount = 0, Note = reason, ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, row.Id, "CancelPending", new { reason, row.VersionNo }, now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(true, "Đã hủy yêu cầu ký quỹ. Bạn có thể tạo đợt ký quỹ mới với số tiền khác.", row.Id);
    }
    public async Task<CampaignFundingResult> SettleAllocationAsync(Guid actor, Guid allocationId, decimal acceptedAmount, string reason, CancellationToken ct = default)
    {
        await using var settlementTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var allocationCampaignId = await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                                          join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                                          where allocation.Id == allocationId
                                          select funding.CampaignId).FirstOrDefaultAsync(ct);
        var adjudicatedCancellation = allocationCampaignId != Guid.Empty && await db.Campaigns.AsNoTracking()
            .AnyAsync(x => x.Id == allocationCampaignId && x.Status == Topstar.Application.Campaigns.CampaignStatuses.Cancelled && (x.CancellationDisputeCaseId != null || (x.CancelledAtUtc != null && !db.Participations.Any(p=>p.CampaignId==x.Id&&p.WorkOpenedAtUtc!=null))), ct);
        if (!adjudicatedCancellation)
            return new(false, "Nghiệm thu áp dụng cho chiến dịch đã hủy theo kết luận tranh chấp hoặc được Brand hủy trước khi mở công việc; cần đối soát quyền lợi KOC trước khi hoàn.");
        reason = (reason ?? string.Empty).Trim(); if (reason.Length < 10 || reason.Length > 500) return new(false, "Lý do nghiệm thu phải từ 10 đến 500 ký tự."); var lockedFundingId=await db.CampaignFundingAllocations.Where(x=>x.Id==allocationId).Select(x=>x.FundingId).FirstOrDefaultAsync(ct); var lockedFunding=await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={lockedFundingId}").SingleOrDefaultAsync(ct); var a = await db.CampaignFundingAllocations.FirstOrDefaultAsync(x => x.Id == allocationId, ct); if (a is null) return new(false, "Không tìm thấy khoản thù lao KOC."); var f = lockedFunding!; if (f.Status is not (CampaignFundingStatuses.Paid or CampaignFundingStatuses.PartiallyReleased)) return new(false, "Quỹ chiến dịch chưa ở trạng thái có thể nghiệm thu."); if (a.ReleasedAtUtc.HasValue) return new(false, "Khoản thù lao này đã được nghiệm thu."); if (acceptedAmount < 0 || acceptedAmount != decimal.Truncate(acceptedAmount) || acceptedAmount > a.Amount) return new(false, "Số tiền nghiệm thu phải từ 0 đến mức thù lao đã chốt."); var p = await db.Participations.FirstAsync(x => x.Id == a.ParticipationId, ct); var now = DateTime.UtcNow; a.AcceptedAmount = acceptedAmount; a.RefundedAmount = a.Amount - acceptedAmount; a.SettlementReason = reason; a.Status = acceptedAmount == a.Amount ? "Accepted" : acceptedAmount == 0 ? "Rejected" : "PartiallyAccepted"; a.ReleasedAtUtc = now; if (acceptedAmount > 0) { var wallet = new WalletTransactionEntity { Id = Guid.NewGuid(), KocProfileId = p.KocProfileId, CampaignId = f.CampaignId, ParticipationId = p.Id, TransactionType = "CampaignEarning", Amount = acceptedAmount, Status = WalletStatuses.Pending, Description = "Thù lao đã nghiệm thu theo quỹ chiến dịch", CreatedByUserId = actor, CreatedAtUtc = now }; db.WalletTransactions.Add(wallet); db.WalletTransactionStatusHistories.Add(new() { Id = Guid.NewGuid(), WalletTransactionId = wallet.Id, ToStatus = WalletStatuses.Pending, Reason = reason, ActorUserId = actor, CreatedAtUtc = now }); a.WalletTransactionId = wallet.Id; db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = f.Id, AllocationId = a.Id, EntryType = "EarningCreated", Amount = -acceptedAmount, Note = reason, ActorUserId = actor, CreatedAtUtc = now }); }
        if (a.RefundedAmount > 0) db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = f.Id, AllocationId = a.Id, EntryType = "RefundApproved", Amount = -a.RefundedAmount, Note = reason, ActorUserId = actor, CreatedAtUtc = now }); f.Status = CampaignFundingStatuses.PartiallyReleased; f.UpdatedAtUtc = now; f.UpdatedByUserId = actor; Audit(actor, f.Id, "SettleAllocation", new { allocationId, acceptedAmount, a.RefundedAmount, reason }, now); await db.SaveChangesAsync(ct); await settlementTx.CommitAsync(ct); return new(true, "Đã ghi nhận kết quả nghiệm thu và số tiền có thể hoàn.", f.Id);
    }
    public async Task<CampaignFundingResult> ConfirmRefundAsync(Guid actor, Guid fundingId, decimal amount, string reference, string reason, CancellationToken ct = default)
    {
        await using var refundTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        reference = (reference ?? string.Empty).Trim(); reason = (reason ?? string.Empty).Trim(); if (reference.Length < 3 || reference.Length > 200) return new(false, "Mã hoàn tiền phải từ 3 đến 200 ký tự."); if (reason.Length < 10 || reason.Length > 500) return new(false, "Lý do hoàn tiền phải từ 10 đến 500 ký tự."); var f = await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={fundingId}").SingleOrDefaultAsync(ct); if (f is null || f.Status is not (CampaignFundingStatuses.Paid or CampaignFundingStatuses.PartiallyReleased)) return new(false, "Quỹ không ở trạng thái có thể hoàn tiền."); var approved = await db.CampaignFundingAllocations.Where(x => x.FundingId == fundingId).SumAsync(x => x.RefundedAmount, ct); if (amount <= 0 || f.RefundedAmount + amount > approved) return new(false, "Số tiền hoàn vượt quá số đã được duyệt hoàn.");if(!await db.CampaignFundingEvidences.AsNoTracking().AnyAsync(x=>x.FundingId==fundingId&&x.EvidenceType==CampaignFundingEvidenceTypes.Refund&&x.Reference==reference&&x.Amount==amount,ct))return new(false,"Cần lưu chứng từ hoàn tiền có cùng mã giao dịch và số tiền trước khi xác nhận.");var approval=await RequireSecondApproverAsync(actor,f.Id,"ConfirmRefund",amount,reference,ct);if(approval is not null){await refundTx.CommitAsync(ct);return approval;} var now = DateTime.UtcNow; f.RefundedAmount += amount; f.RefundReference = reference; f.RefundedAtUtc = now; f.Status = f.RefundedAmount == approved && await db.CampaignFundingAllocations.Where(x => x.FundingId == fundingId).AllAsync(x => x.ReleasedAtUtc != null, ct) ? CampaignFundingStatuses.Refunded : CampaignFundingStatuses.PartiallyReleased; f.UpdatedAtUtc = now; f.UpdatedByUserId = actor; db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = f.Id, EntryType = "RefundPaid", Amount = -amount, Note = $"{reference}: {reason}", ActorUserId = actor, CreatedAtUtc = now }); Audit(actor, f.Id, "ConfirmRefund", new { amount, reference, reason }, now); await db.SaveChangesAsync(ct); await refundTx.CommitAsync(ct); return new(true, "Đã ghi nhận giao dịch hoàn tiền.", f.Id);
    }

    public async Task<CampaignFundingResult> AddAdminEvidenceAsync(Guid actor, Guid fundingId, string evidenceType, string fileUrl, string reference, decimal? amount, string note, CancellationToken ct = default)
    {
        evidenceType = (evidenceType ?? string.Empty).Trim(); fileUrl = (fileUrl ?? string.Empty).Trim(); reference = (reference ?? string.Empty).Trim(); note = (note ?? string.Empty).Trim();
        if (evidenceType is not (CampaignFundingEvidenceTypes.Reconciliation or CampaignFundingEvidenceTypes.Refund)) return new(false, "Loại chứng từ không hợp lệ.");
        if (!fileUrl.StartsWith("/uploads/campaign-funding-evidence/", StringComparison.OrdinalIgnoreCase)) return new(false, "Tệp chứng từ không hợp lệ.");
        if (reference.Length < 3 || reference.Length > 200) return new(false, "Mã giao dịch phải từ 3 đến 200 ký tự.");
        if (note.Length < 10 || note.Length > 500) return new(false, "Ghi chú chứng từ phải từ 10 đến 500 ký tự.");
        var funding = await db.CampaignFundings.FirstOrDefaultAsync(x => x.Id == fundingId, ct);
        if (funding is null) return new(false, "Không tìm thấy hồ sơ ký quỹ.");
        if (evidenceType == CampaignFundingEvidenceTypes.Reconciliation && funding.PaymentMethod != CampaignPaymentMethods.BankTransfer) return new(false, "Chứng từ đối soát thủ công chỉ áp dụng cho chuyển khoản ngân hàng.");
        if (evidenceType == CampaignFundingEvidenceTypes.Refund)
        {
            var approvedRefund = await db.CampaignFundingAllocations.Where(x => x.FundingId == fundingId).SumAsync(x => x.RefundedAmount, ct);
            if (!amount.HasValue || amount <= 0 || funding.RefundedAmount + amount > approvedRefund) return new(false, "Số tiền trên chứng từ hoàn vượt quá số đã được duyệt hoàn.");
        }
        if (evidenceType == CampaignFundingEvidenceTypes.Reconciliation && (!amount.HasValue || amount <= 0)) return new(false, "Vui lòng nhập số tiền thực nhận trên chứng từ đối soát.");
        var now = DateTime.UtcNow;
        db.CampaignFundingEvidences.Add(new() { Id = Guid.NewGuid(), FundingId = fundingId, EvidenceType = evidenceType, FileUrl = fileUrl, Reference = reference, Amount = amount, Note = note, ActorUserId = actor, CreatedAtUtc = now });
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = fundingId, EntryType = evidenceType == CampaignFundingEvidenceTypes.Refund ? "RefundEvidenceAdded" : "ReconciliationEvidenceAdded", Amount = amount ?? 0, Note = $"{reference}: {note}", ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, fundingId, "AddCampaignFundingEvidence", new { evidenceType, reference, amount, NoteLength = note.Length }, now);
        await db.SaveChangesAsync(ct);
        return new(true, evidenceType == CampaignFundingEvidenceTypes.Refund ? "Đã lưu chứng từ hoàn tiền." : "Đã lưu chứng từ đối soát.", fundingId);
    }
    public async Task<CampaignFundingResult> ReviewEvidenceAsync(Guid actor, Guid evidenceId, string status, string reason, CancellationToken ct = default)
    {
        status = (status ?? string.Empty).Trim(); reason = (reason ?? string.Empty).Trim();
        if (status is not (CampaignFundingEvidenceReviewStatuses.Accepted or CampaignFundingEvidenceReviewStatuses.SupplementRequired or CampaignFundingEvidenceReviewStatuses.Rejected)) return new(false, "Kết quả kiểm tra chứng từ không hợp lệ.");
        if (reason.Length is < 10 or > 500) return new(false, "Nội dung phản hồi chứng từ phải từ 10 đến 500 ký tự.");
        var evidence = await db.CampaignFundingEvidences.AsNoTracking().FirstOrDefaultAsync(x => x.Id == evidenceId, ct);
        if (evidence is null) return new(false, "Không tìm thấy phiên bản chứng từ.");
        if (evidence.EvidenceType != CampaignFundingEvidenceTypes.BrandPayment) return new(false, "Chỉ xét duyệt chứng từ do Brand gửi.");
        var now = DateTime.UtcNow;
        db.CampaignFundingEvidenceReviews.Add(new() { Id = Guid.NewGuid(), EvidenceId = evidenceId, Status = status, Reason = reason, ActorUserId = actor, CreatedAtUtc = now });
        var entryType = status == CampaignFundingEvidenceReviewStatuses.Accepted ? "EvidenceAccepted" : status == CampaignFundingEvidenceReviewStatuses.SupplementRequired ? "EvidenceSupplementRequired" : "EvidenceRejected";
        db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = evidence.FundingId, EntryType = entryType, Amount = 0, Note = reason, ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, evidence.FundingId, "ReviewCampaignFundingEvidence", new { evidenceId, status, reason }, now);
        await db.SaveChangesAsync(ct);
        return new(true, status == CampaignFundingEvidenceReviewStatuses.Accepted ? "Đã chấp nhận chứng từ." : status == CampaignFundingEvidenceReviewStatuses.SupplementRequired ? "Đã yêu cầu Brand bổ sung chứng từ." : "Đã từ chối chứng từ.", evidence.FundingId);
    }
    public Task<bool> IsFundedAsync(Guid campaignId, CancellationToken ct = default) => db.CampaignFundings.AsNoTracking().AnyAsync(x => x.CampaignId == campaignId && (x.Status == CampaignFundingStatuses.Paid || x.Status == CampaignFundingStatuses.PartiallyReleased), ct);
    public async Task<CampaignFundingResult> CreateEarningsAsync(Guid actor, Guid campaignId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var fundings = await db.CampaignFundings.Where(x => x.CampaignId == campaignId && (x.Status == CampaignFundingStatuses.Paid || x.Status == CampaignFundingStatuses.PartiallyReleased)).ToListAsync(ct);
        if (fundings.Count == 0) return new(false, "Chiến dịch chưa có đợt ký quỹ đã thanh toán.");
        var fundingIds=fundings.Select(x=>x.Id).ToArray();var candidates = await db.CampaignFundingAllocations.Where(x => fundingIds.Contains(x.FundingId) && x.ReleasedAtUtc == null).ToListAsync(ct);
        if (candidates.Count == 0) return new(true, "Thu nhập KOC đã được tạo trước đó.", fundings.OrderByDescending(x=>x.VersionNo).First().Id);
        var parts = await db.Participations.Where(x => x.CampaignId == campaignId).ToDictionaryAsync(x => x.Id, ct);
        var campaign = await db.Campaigns.AsNoTracking().FirstAsync(x => x.Id == campaignId, ct);
        var allocations = new List<CampaignFundingAllocationEntity>();
        foreach (var candidate in candidates)
        {
            if (!parts.ContainsKey(candidate.ParticipationId)) return new(false, "Phân bổ có KOC không còn thuộc chiến dịch.");
            var submissionStatuses = await db.Submissions.AsNoTracking().Where(x => x.ParticipationId == candidate.ParticipationId && x.IsVisible).Select(x => x.Status).ToListAsync(ct);
            if (submissionStatuses.Count == 0 || submissionStatuses.Any(x => x != SubmissionStatuses.Approved)) continue;
            if (campaign.RequiresBooking && !await db.Schedules.AsNoTracking().AnyAsync(x => x.ParticipationId == candidate.ParticipationId && x.Status == "Confirmed", ct)) continue;
            if (campaign.RequiresShipment && !await db.Shipments.AsNoTracking().AnyAsync(x => x.ParticipationId == candidate.ParticipationId && x.Status == "Delivered", ct)) continue;
            if (await db.DisputeCases.AsNoTracking().AnyAsync(x => x.ParticipationId == candidate.ParticipationId && x.IsActive, ct)) continue;
            allocations.Add(candidate);
        }
        if (allocations.Count == 0) return new(false, "Chưa có KOC nào hoàn thành đầy đủ đầu việc để tạo thu nhập.");
        var now = DateTime.UtcNow;
        foreach (var a in allocations) { var part=parts[a.ParticipationId]; var wallet = new WalletTransactionEntity { Id = Guid.NewGuid(), KocProfileId = part.KocProfileId, CampaignId = campaignId, ParticipationId = part.Id, TransactionType = "CampaignEarning", Amount = a.Amount, Status = WalletStatuses.Pending, Description = "Thù lao theo bảng phân bổ đã ký quỹ", CreatedByUserId = actor, CreatedAtUtc = now }; db.WalletTransactions.Add(wallet); db.WalletTransactionStatusHistories.Add(new() { Id = Guid.NewGuid(), WalletTransactionId = wallet.Id, ToStatus = WalletStatuses.Pending, Reason = "KOC đã hoàn thành và được nghiệm thu", ActorUserId = actor, CreatedAtUtc = now }); a.AcceptedAmount=a.Amount;a.Status="Accepted";a.SettlementReason="KOC đã hoàn thành toàn bộ đầu việc và được nghiệm thu.";a.ReleasedAtUtc = now; a.WalletTransactionId = wallet.Id; db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = a.FundingId, AllocationId = a.Id, EntryType = "EarningCreated", Amount = -a.Amount, Note = part.Id.ToString(), ActorUserId = actor, CreatedAtUtc = now }); }
        foreach(var f in fundings.Where(f=>allocations.Any(a=>a.FundingId==f.Id))){var remaining=candidates.Any(a=>a.FundingId==f.Id&&!allocations.Any(done=>done.Id==a.Id));f.Status=remaining?CampaignFundingStatuses.PartiallyReleased:CampaignFundingStatuses.Released;f.UpdatedAtUtc=now;f.UpdatedByUserId=actor;Audit(actor,f.Id,"CreateBatchEarnings",new{Count=allocations.Count(x=>x.FundingId==f.Id),Amount=allocations.Where(x=>x.FundingId==f.Id).Sum(x=>x.Amount)},now);}
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(true, "Đã tạo thu nhập chờ đối soát cho các KOC thuộc những đợt đã ký quỹ.", fundings.OrderByDescending(x=>x.VersionNo).First().Id);
    }
    private async Task ExpirePendingAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow; var rows = await db.CampaignFundings.Where(x => x.Status == CampaignFundingStatuses.Pending && x.DueAtUtc < now).ToListAsync(ct); if (rows.Count == 0) return; foreach (var f in rows) { f.Status = CampaignFundingStatuses.Cancelled; f.CancelledAtUtc = now; f.CancellationReason = "Yêu cầu thanh toán tự động hết hạn."; f.UpdatedAtUtc = now; db.CampaignFundLedgers.Add(new() { Id = Guid.NewGuid(), FundingId = f.Id, EntryType = "Expired", Amount = 0, Note = f.CancellationReason, CreatedAtUtc = now }); }
        await db.SaveChangesAsync(ct);
    }
    private async Task<CampaignFundingResult?> RequireSecondApproverAsync(Guid actor,Guid fundingId,string actionType,decimal amount,string reference,CancellationToken ct)
    {
        var threshold=await db.CampaignFeePolicies.AsNoTracking().Where(x=>x.Code=="DEFAULT"&&x.IsActive&&x.EffectiveAtUtc<=DateTime.UtcNow).OrderByDescending(x=>x.EffectiveAtUtc).Select(x=>x.DualApprovalThreshold).FirstOrDefaultAsync(ct);
        if(amount<threshold)return null;
        var pending=await db.CampaignFundingApprovals.FirstOrDefaultAsync(x=>x.FundingId==fundingId&&x.ActionType==actionType&&x.Status=="Pending",ct);
        var now=DateTime.UtcNow;
        if(pending is null)
        {
            db.CampaignFundingApprovals.Add(new(){Id=Guid.NewGuid(),FundingId=fundingId,ActionType=actionType,Amount=amount,Reference=reference,RequestedByUserId=actor,CreatedAtUtc=now});
            db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=fundingId,EntryType="ApprovalRequested",Amount=amount,Note=reference,ActorUserId=actor,CreatedAtUtc=now});
            Audit(actor,fundingId,"RequestSecondApproval",new{actionType,amount,reference},now);await db.SaveChangesAsync(ct);
            return new(true,$"Giao dịch từ {threshold:N0} ₫ cần hai cấp duyệt. Yêu cầu đã được chuyển cho quản trị viên khác.",fundingId);
        }
        if(pending.RequestedByUserId==actor)return new(false,"Giao dịch này đang chờ một quản trị viên khác phê duyệt.",fundingId);
        if(pending.Amount!=amount||!string.Equals(pending.Reference,reference,StringComparison.Ordinal))return new(false,"Thông tin giao dịch không khớp yêu cầu phê duyệt đang chờ.",fundingId);
        pending.Status="Approved";pending.DecidedByUserId=actor;pending.DecidedAtUtc=now;
        db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=fundingId,EntryType="ApprovalGranted",Amount=amount,Note=reference,ActorUserId=actor,CreatedAtUtc=now});
        Audit(actor,fundingId,"GrantSecondApproval",new{actionType,amount,reference},now);
        return null;
    }
    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now) => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "CampaignFunding", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });
}


