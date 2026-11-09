using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using Topstar.Application.Campaigns;
using Topstar.Application.Deliverables;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Finance;

public sealed class WalletService(TopstarDbContext db, IDataProtectionProvider dataProtectionProvider) : IWalletService
{
    private readonly IDataProtector payoutProtector = dataProtectionProvider.CreateProtector("Topstar.PayoutAccount.v1");
    private readonly IDataProtector payoutOtpProtector = dataProtectionProvider.CreateProtector("Topstar.PayoutOtp.v2");
    public async Task<IReadOnlyList<AdminSettlementItem>> GetAdminSettlementsAsync(CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var rows = await (from participation in db.Participations.AsNoTracking()
                  join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                  join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                  let transaction = db.WalletTransactions.AsNoTracking().FirstOrDefault(x => x.ParticipationId == participation.Id)
                  let approvedCount = db.Submissions.Count(x => x.ParticipationId == participation.Id && x.Status == SubmissionStatuses.Approved && x.IsVisible)
                  where campaign.Status == CampaignStatuses.Completed && participation.Status == CampaignStatuses.Completed && approvedCount > 0
                  orderby transaction == null ? 0 : 1, campaign.UpdatedAtUtc descending, koc.DisplayName
                  select new { participation.Id, CampaignId = campaign.Id, CampaignTitle = campaign.Title, KocName = koc.DisplayName,
                      campaign.BudgetAmount, ApprovedCount = approvedCount, TransactionId = transaction == null ? (Guid?)null : transaction.Id,
                      Amount = transaction == null ? (decimal?)null : transaction.Amount, Status = transaction == null ? "NotCreated" : transaction.Status,
                      HoldReason = transaction == null ? null : transaction.HoldReason })
            .ToWindowAsync(window, cancellationToken,
                q => q.Where(x => (window!.Search == null || x.CampaignTitle.Contains(window.Search) || x.KocName.Contains(window.Search)) &&
                                   (window.Status == null || x.Status == window.Status)), x => x.Status);
        return rows.Select(x => new AdminSettlementItem(x.Id, x.CampaignId, x.CampaignTitle, x.KocName, x.BudgetAmount,
            x.ApprovedCount, x.TransactionId, x.Amount, x.Status == "NotCreated" ? null : x.Status, x.HoldReason)).ToList();
    }

    public async Task<WalletResult> CreateEarningAsync(Guid adminUserId, Guid participationId, decimal amount, string? description, CancellationToken cancellationToken = default)
    {
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (!WalletRules.IsValidAmount(amount)) return new(false, "Số tiền đối soát phải lớn hơn 0.");
        if (description?.Length > 500) return new(false, "Ghi chú không được vượt quá 500 ký tự.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var row = await (from participation in db.Participations
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         where participation.Id == participationId
                         select new { Participation = participation, Campaign = campaign, Koc = koc }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy participation.");
        if (row.Campaign.Status != CampaignStatuses.Completed || row.Participation.Status != CampaignStatuses.Completed)
            return new(false, "Chỉ đối soát participation đã hoàn tất.");
        if (await db.CampaignFundings.AsNoTracking().AnyAsync(x=>x.CampaignId==row.Campaign.Id,cancellationToken))
            return new(false,"Chiến dịch đã dùng quỹ ký quỹ; thu nhập KOC phải lấy tự động từ bảng phân bổ, Admin không được nhập lại.");
        if (!await db.Submissions.AsNoTracking().AnyAsync(x => x.ParticipationId == participationId && x.Status == SubmissionStatuses.Approved && x.IsVisible, cancellationToken))
            return new(false, "Participation chưa có submission Approved.");
        if (await db.WalletTransactions.AnyAsync(x => x.ParticipationId == participationId, cancellationToken))
            return new(false, "Participation này đã có khoản đối soát.");
        var allocated = await db.WalletTransactions.Where(x => x.CampaignId == row.Campaign.Id && x.TransactionType == "Earning" && x.Status != WalletStatuses.Failed)
            .SumAsync(x => (decimal?)(x.GrossAmountSnapshot??x.Amount), cancellationToken) ?? 0m;
        if (allocated + amount > row.Campaign.BudgetAmount)
            return new(false, $"Tổng đối soát vượt ngân sách campaign còn lại {(row.Campaign.BudgetAmount - allocated):N0} ₫.");

        var now = DateTime.UtcNow;
        var wallet = new WalletTransactionEntity { Id = Guid.NewGuid(), KocProfileId = row.Koc.Id, CampaignId = row.Campaign.Id, ParticipationId = participationId, TransactionType = "Earning", Amount = amount, Status = WalletStatuses.Pending, Description = description, CreatedByUserId = adminUserId, CreatedAtUtc = now };
        db.WalletTransactions.Add(wallet);
        db.WalletTransactionStatusHistories.Add(new WalletTransactionStatusHistoryEntity { Id = Guid.NewGuid(), WalletTransactionId = wallet.Id, FromStatus = null, ToStatus = WalletStatuses.Pending, Reason = description, ActorUserId = adminUserId, CreatedAtUtc = now });
        Audit(adminUserId, wallet.Id, "CreateEarning", new { participationId, row.Campaign.Id, Amount = amount }, now);
        Notify(row.Koc.UserId, "Wallet", "Khoản thu nhập đang đối soát", $"Khoản thu nhập {amount:N0} ₫ từ chiến dịch {row.Campaign.Title} đang chờ khả dụng.", "WalletTransaction", wallet.Id, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Dữ liệu đối soát vừa thay đổi. Vui lòng tải lại trang.");
        }
        return new(true, "Đã tạo khoản thu nhập Pending.", wallet.Id);
    }

    public async Task<WalletResult> ChangeStatusAsync(Guid adminUserId, Guid transactionId, string targetStatus, string? reason, CancellationToken cancellationToken = default)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var wallet = await db.WalletTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
        if (wallet is null) return new(false, "Không tìm thấy giao dịch ví.");
        if(wallet.TaxState=="Awaiting"&&targetStatus!=WalletStatuses.OnHold)return new(false,"Khoản thu nhập đang chờ xác minh thuế. Vui lòng xử lý tại Thuế thu nhập KOC.");
        if (!WalletRules.CanTransition(wallet.Status, targetStatus)) return new(false, $"Không thể chuyển {wallet.Status} sang {targetStatus}.");
        if (targetStatus == WalletStatuses.OnHold && reason is null) return new(false, "Vui lòng nhập lý do tạm giữ.");
        if (targetStatus==WalletStatuses.Available&&wallet.Status==WalletStatuses.Pending)
        {
            var holdDays=await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                                join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                                where allocation.WalletTransactionId==wallet.Id
                                select (int?)funding.DisputeHoldDaysSnapshot).FirstOrDefaultAsync(cancellationToken);
            holdDays??=await db.CampaignFundings.AsNoTracking().Where(x=>x.CampaignId==wallet.CampaignId).OrderByDescending(x=>x.VersionNo).Select(x=>(int?)x.DisputeHoldDaysSnapshot).FirstOrDefaultAsync(cancellationToken);
            if(holdDays.HasValue&&wallet.CreatedAtUtc.AddDays(holdDays.Value)>DateTime.UtcNow)return new(false,$"Khoản thu nhập còn trong thời gian chờ khiếu nại {holdDays.Value} ngày.");
            if(await db.DisputeCases.AsNoTracking().AnyAsync(x=>x.ParticipationId==wallet.ParticipationId&&x.IsActive,cancellationToken))return new(false,"Khoản thu nhập đang có tranh chấp nên chưa thể chuyển sang khả dụng.");
        }
        if (reason?.Length > 500) return new(false, "Lý do không được vượt quá 500 ký tự.");

        var now = DateTime.UtcNow;
        var before = wallet.Status;
        var kocUserId = await db.KocProfiles.AsNoTracking().Where(x => x.Id == wallet.KocProfileId).Select(x => x.UserId).FirstAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.WalletTransactions.Where(x => x.Id == transactionId && x.Status == before)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, targetStatus)
                .SetProperty(x => x.HoldReason, targetStatus == WalletStatuses.OnHold ? reason : null)
                .SetProperty(x => x.HoldDisputeCaseId, (Guid?)null)
                .SetProperty(x => x.AvailableAtUtc, targetStatus == WalletStatuses.Available ? now : null)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        if (affected != 1) return new(false, "Trạng thái giao dịch vừa thay đổi. Vui lòng tải lại trang.");
        db.WalletTransactionStatusHistories.Add(new WalletTransactionStatusHistoryEntity { Id = Guid.NewGuid(), WalletTransactionId = wallet.Id, FromStatus = before, ToStatus = targetStatus, Reason = reason, ActorUserId = adminUserId, DisputeCaseId = wallet.HoldDisputeCaseId, CreatedAtUtc = now });
        Audit(adminUserId, wallet.Id, "ChangeStatus", new { Before = before, After = targetStatus, Reason = reason }, now);
        Notify(kocUserId, "Wallet", targetStatus == WalletStatuses.Available ? "Thu nhập đã khả dụng" : "Thu nhập đang tạm giữ", targetStatus == WalletStatuses.Available ? $"Khoản thu nhập {wallet.Amount:N0} ₫ đã khả dụng." : $"Khoản thu nhập {wallet.Amount:N0} ₫ đang tạm giữ: {reason}", "WalletTransaction", wallet.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, targetStatus == WalletStatuses.Available ? "Đã chuyển khoản thu nhập sang Available." : "Đã tạm giữ khoản thu nhập.", wallet.Id);
    }

    public async Task<KocWalletOverview?> GetKocWalletAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? transactions = null, Topstar.Application.Common.ListWindow? payouts = null)
    {
        var kocId = await db.KocProfiles.AsNoTracking().Where(x => x.UserId == kocUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!kocId.HasValue) return null;
        var walletQuery = db.WalletTransactions.AsNoTracking().Where(x => x.KocProfileId == kocId.Value);
        var payoutQuery = db.PayoutRequests.AsNoTracking().Where(x => x.KocProfileId == kocId.Value);
        var walletTotals = await walletQuery.GroupBy(x => x.Status).Select(g => new { Status=g.Key, Amount=g.Sum(x=>x.Amount) }).ToDictionaryAsync(x=>x.Status,x=>x.Amount,cancellationToken);
        var payoutTotals = await payoutQuery.GroupBy(x => x.Status).Select(g => new { Status=g.Key, Amount=g.Sum(x=>x.Amount) }).ToDictionaryAsync(x=>x.Status,x=>x.Amount,cancellationToken);
        var rawRows = await (from wallet in walletQuery join campaign in db.Campaigns.AsNoTracking() on wallet.CampaignId equals campaign.Id orderby wallet.CreatedAtUtc descending, wallet.Id select new { wallet, campaign.Title })
            .ToWindowAsync(transactions,cancellationToken,q=>q.Where(x=>(transactions!.Search==null||x.Title.Contains(transactions.Search)||(x.wallet.Description!=null&&x.wallet.Description.Contains(transactions.Search)))&&(transactions.Status==null||x.wallet.Status==transactions.Status)),x=>x.wallet.Status);
        var rows = rawRows.Select(x=>new WalletTransactionItem(x.wallet.Id,x.Title,x.wallet.Amount,x.wallet.Status,x.wallet.Description,x.wallet.HoldReason,x.wallet.CreatedAtUtc,x.wallet.AvailableAtUtc){GrossAmount=x.wallet.GrossAmountSnapshot,PlatformFeeAmount=x.wallet.PlatformFeeAmountSnapshot,TaxAmount=x.wallet.TaxAmountSnapshot,TaxState=x.wallet.TaxState}).ToList();
        var rawPayouts = await payoutQuery.OrderByDescending(x=>x.RequestedAtUtc).ThenBy(x=>x.Id).ToWindowAsync(payouts,cancellationToken,q=>q.Where(x=>(payouts!.Search==null||x.BankCode.Contains(payouts.Search)||(x.PayoutReference!=null&&x.PayoutReference.Contains(payouts.Search)))&&(payouts.Status==null||x.Status==payouts.Status)),x=>x.Status);
        var payoutRows = rawPayouts.Select(x=>new PayoutRequestItem(x.Id,x.Amount,x.FeeAmountSnapshot,x.NetAmountSnapshot,x.Status,x.BankCode,x.AccountNumberLast4,x.PayoutReference,x.FailureReason,x.RequestedAtUtc,x.ProcessedAtUtc)).ToList();
        var walletIds = rows.Select(x => x.Id).ToArray();
        var walletHistoryRows = await db.WalletTransactionStatusHistories.AsNoTracking().Where(x => walletIds.Contains(x.WalletTransactionId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        rows = rows.Select(x => x with { History = walletHistoryRows.Where(h => h.WalletTransactionId == x.Id).Select(h => new StatusHistoryItem(h.FromStatus, h.ToStatus, h.Reason, h.CreatedAtUtc)).ToList() }).ToList();
        var payoutIds = payoutRows.Select(x => x.Id).ToArray();
        var payoutHistoryRows = await db.PayoutRequestStatusHistories.AsNoTracking().Where(x => payoutIds.Contains(x.PayoutRequestId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        payoutRows = payoutRows.Select(x => x with { History = payoutHistoryRows.Where(h => h.PayoutRequestId == x.Id).Select(h => new StatusHistoryItem(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList() }).ToList();
        var reserved = payoutTotals.GetValueOrDefault(PayoutStatuses.WithdrawRequested) + payoutTotals.GetValueOrDefault(PayoutStatuses.PaidOut);
        var grossAvailable = walletTotals.GetValueOrDefault(WalletStatuses.Available);
        var accounts = await db.KocPayoutAccounts.AsNoTracking()
            .Where(x => x.KocProfileId == kocId.Value && x.DeletedAtUtc == null)
            .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new PayoutAccountSummary(x.Id, x.BankCode, x.AccountHolderName, $"•••• {x.AccountNumberLast4}", x.IsDefault))
            .ToListAsync(cancellationToken);
        var policy=await GetPayoutPolicyAsync(cancellationToken);return new(Math.Max(0, grossAvailable - reserved), walletTotals.GetValueOrDefault(WalletStatuses.Pending),
            walletTotals.GetValueOrDefault(WalletStatuses.OnHold), payoutTotals.GetValueOrDefault(PayoutStatuses.WithdrawRequested),
            walletTotals.Where(x=>x.Key!=WalletStatuses.Failed).Sum(x=>x.Value), accounts, rows, payoutRows,policy);
    }

    public async Task<WalletResult> SavePayoutAccountAsync(Guid kocUserId, Guid? accountId, string bankCode, string accountHolderName, string accountNumber, bool makeDefault, CancellationToken cancellationToken = default)
    {
        bankCode = bankCode?.Trim().ToUpperInvariant() ?? string.Empty;
        accountHolderName = accountHolderName?.Trim().ToUpperInvariant() ?? string.Empty;
        accountNumber = (accountNumber ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        if (bankCode.Length is < 2 or > 20 || !bankCode.All(char.IsLetterOrDigit)) return new(false, "Mã ngân hàng gồm 2–20 ký tự chữ hoặc số.");
        if (accountHolderName.Length is < 2 or > 150) return new(false, "Tên chủ tài khoản không hợp lệ.");
        if (accountNumber.Length is < 6 or > 30 || !accountNumber.All(char.IsDigit)) return new(false, "Số tài khoản phải có từ 6 đến 30 chữ số.");
        var kocId = await db.KocProfiles.Where(x => x.UserId == kocUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!kocId.HasValue) return new(false, "Không tìm thấy hồ sơ KOC.");
        var now = DateTime.UtcNow;
        var entity = accountId.HasValue
            ? await db.KocPayoutAccounts.FirstOrDefaultAsync(x => x.Id == accountId.Value && x.KocProfileId == kocId.Value && x.DeletedAtUtc == null, cancellationToken)
            : null;
        if (accountId.HasValue && entity is null) return new(false, "Tài khoản nhận tiền không còn tồn tại.");
        if (entity is null)
        {
            var activeCount = await db.KocPayoutAccounts.CountAsync(x => x.KocProfileId == kocId.Value && x.DeletedAtUtc == null, cancellationToken);
            var maxAccounts=await db.OperationalRuntimePolicies.AsNoTracking().Select(x=>x.MaxPayoutAccounts).SingleAsync(cancellationToken);
            if (activeCount >= maxAccounts) return new(false, $"Mỗi KOC được lưu tối đa {maxAccounts} tài khoản nhận tiền.");
            entity = new KocPayoutAccountEntity { Id = Guid.NewGuid(), KocProfileId = kocId.Value, CreatedAtUtc = now, IsDefault = activeCount == 0 || makeDefault };
            db.KocPayoutAccounts.Add(entity);
        }
        if (makeDefault)
        {
            var currentDefaults = await db.KocPayoutAccounts.Where(x => x.KocProfileId == kocId.Value && x.DeletedAtUtc == null && x.IsDefault && x.Id != entity.Id).ToListAsync(cancellationToken);
            currentDefaults.ForEach(x => { x.IsDefault = false; x.UpdatedAtUtc = now; });
            entity.IsDefault = true;
        }
        entity.BankCode = bankCode; entity.AccountHolderName = accountHolderName; entity.AccountNumberCiphertext = payoutProtector.Protect(accountNumber); entity.AccountNumberLast4 = accountNumber[^4..]; entity.UpdatedAtUtc = now;
        Audit(kocUserId, entity.Id, accountId.HasValue ? "UpdatePayoutAccount" : "CreatePayoutAccount", new { BankCode = bankCode, AccountNumberLast4 = entity.AccountNumberLast4, entity.IsDefault }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã lưu tài khoản nhận tiền an toàn.", entity.Id);
    }

    public async Task<WalletResult> SetDefaultPayoutAccountAsync(Guid kocUserId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var accounts = await (from account in db.KocPayoutAccounts
                              join koc in db.KocProfiles on account.KocProfileId equals koc.Id
                              where koc.UserId == kocUserId && account.DeletedAtUtc == null
                              select account).ToListAsync(cancellationToken);
        var selected = accounts.FirstOrDefault(x => x.Id == accountId);
        if (selected is null) return new(false, "Tài khoản nhận tiền không còn tồn tại.");
        var now = DateTime.UtcNow;
        foreach (var account in accounts) { account.IsDefault = account.Id == accountId; account.UpdatedAtUtc = now; }
        Audit(kocUserId, accountId, "SetDefaultPayoutAccount", new { selected.BankCode, selected.AccountNumberLast4 }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã đổi tài khoản nhận tiền mặc định.", accountId);
    }

    public async Task<WalletResult> DeletePayoutAccountAsync(Guid kocUserId, Guid accountId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            return new(false, "Lý do xóa tài khoản phải có từ 10 đến 500 ký tự.");

        var entity = await (from account in db.KocPayoutAccounts
                            join koc in db.KocProfiles on account.KocProfileId equals koc.Id
                            where koc.UserId == kocUserId && account.Id == accountId && account.DeletedAtUtc == null
                            select account)
            .SingleOrDefaultAsync(cancellationToken);
        if (entity is null)
            return new(false, "Tài khoản nhận tiền không còn tồn tại.");

        var now = DateTime.UtcNow;
        Audit(kocUserId, entity.Id, "DeletePayoutAccount", new
        {
            entity.BankCode,
            entity.AccountNumberLast4,
            Reason = reason
        }, now);
        entity.DeletedAtUtc = now;
        entity.DeletedReason = reason;
        entity.IsDefault = false;
        entity.UpdatedAtUtc = now;
        var replacement = await db.KocPayoutAccounts
            .Where(x => x.KocProfileId == entity.KocProfileId && x.Id != entity.Id && x.DeletedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (replacement is not null) { replacement.IsDefault = true; replacement.UpdatedAtUtc = now; }
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã xóa tài khoản nhận tiền. Lịch sử yêu cầu rút tiền vẫn được giữ nguyên.");
    }

    public Task<WalletResult> RequestPayoutAsync(Guid kocUserId, decimal amount, CancellationToken cancellationToken = default)
        => Task.FromResult(new WalletResult(false, "Yêu cầu rút tiền phải được xác thực bằng OTP."));

    public async Task<PayoutOtpChallengeResult> CreatePayoutOtpChallengeAsync(Guid kocUserId, Guid payoutAccountId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (!WalletRules.IsValidAmount(amount))
            return new(false, "Số tiền rút phải là số nguyên đồng hợp lệ và lớn hơn 0.");
        var owner = await (from koc in db.KocProfiles
                           join user in db.Users on koc.UserId equals user.Id
                           where koc.UserId == kocUserId
                           select new { KocId = koc.Id, user.Email, user.EmailConfirmed })
            .SingleOrDefaultAsync(cancellationToken);
        if (owner is null) return new(false, "Không tìm thấy hồ sơ KOC.");
        if (!owner.EmailConfirmed || string.IsNullOrWhiteSpace(owner.Email))
            return new(false, "Bạn cần xác minh email trước khi rút tiền.");

        var policy = await db.PayoutPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.Code == "DEFAULT", cancellationToken);
        if (policy is null) return new(false, "Hệ thống chưa cấu hình chính sách rút tiền.");
        if (amount < policy.MinimumAmount) return new(false, $"Số tiền rút tối thiểu là {policy.MinimumAmount:N0} ₫.");
        if (PayoutPolicyRules.CalculateFee(amount, policy.FixedFee, policy.FeeRate) >= amount)
            return new(false, "Phí rút tiền phải nhỏ hơn số tiền yêu cầu.");
        if (!await db.KocPayoutAccounts.AsNoTracking().AnyAsync(x => x.Id == payoutAccountId && x.KocProfileId == owner.KocId && x.DeletedAtUtc == null, cancellationToken))
            return new(false, "Vui lòng chọn tài khoản nhận tiền hợp lệ.");

        var grossAvailable = await db.WalletTransactions
            .Where(x => x.KocProfileId == owner.KocId && x.Status == WalletStatuses.Available)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var reserved = await db.PayoutRequests
            .Where(x => x.KocProfileId == owner.KocId && (x.Status == PayoutStatuses.WithdrawRequested || x.Status == PayoutStatuses.PaidOut))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        if (amount > grossAvailable - reserved)
            return new(false, $"Số dư khả dụng không đủ. Hiện còn {(grossAvailable - reserved):N0} ₫.");

        var now = DateTime.UtcNow;
        var vietnamNow = now.AddHours(7);
        var dayStartUtc = vietnamNow.Date.AddHours(-7);
        var dayEndUtc = dayStartUtc.AddDays(1);
        var payoutCount = await db.PayoutRequests.CountAsync(
            x => x.KocProfileId == owner.KocId && x.RequestedAtUtc >= dayStartUtc && x.RequestedAtUtc < dayEndUtc,
            cancellationToken);
        if (payoutCount >= policy.DailyRequestLimit)
            return new(false, $"Bạn đã đạt giới hạn {policy.DailyRequestLimit} yêu cầu rút trong ngày.");

        var otpPolicy=await db.OperationalRuntimePolicies.AsNoTracking().SingleAsync(cancellationToken);
        var recentCount = await db.PayoutOtpChallenges.CountAsync(
            x => x.KocProfileId == owner.KocId && x.CreatedAtUtc >= now.AddMinutes(-otpPolicy.OtpWindowMinutes), cancellationToken);
        if (recentCount >= otpPolicy.OtpRequestLimit) return new(false, $"Bạn đã yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau {otpPolicy.OtpWindowMinutes} phút.");

        var challengeId = Guid.NewGuid();
        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challenge = new PayoutOtpChallengeEntity
        {
            Id = challengeId, KocProfileId = owner.KocId, Amount = amount, PayoutAccountId = payoutAccountId,
            CodeHash = ProtectOtp(challengeId, rawCode), AttemptCount = 0,
            ExpiresAtUtc = now.AddMinutes(otpPolicy.OtpExpiryMinutes), CreatedAtUtc = now
        };
        db.PayoutOtpChallenges.Add(challenge);
        Audit(kocUserId, challengeId, "CreatePayoutOtpChallenge", new { Amount = amount, PayoutAccountId = payoutAccountId, challenge.ExpiresAtUtc }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Mã OTP đã được tạo.", challengeId, rawCode, owner.Email, challenge.ExpiresAtUtc);
    }

    public async Task<WalletResult> ConfirmPayoutOtpAsync(Guid kocUserId, Guid challengeId, string code, CancellationToken cancellationToken = default)
    {
        code = code?.Trim() ?? string.Empty;
        if (!PayoutOtpRules.IsValidCode(code)) return new(false, "OTP phải gồm đúng 6 chữ số.");
        var challenge = await (from otp in db.PayoutOtpChallenges.AsNoTracking()
                               join koc in db.KocProfiles.AsNoTracking() on otp.KocProfileId equals koc.Id
                               where otp.Id == challengeId && koc.UserId == kocUserId
                               select otp).SingleOrDefaultAsync(cancellationToken);
        if (challenge is null) return new(false, "Không tìm thấy yêu cầu xác thực.");
        if (challenge.ConsumedAtUtc.HasValue) return new(false, "Mã OTP đã được sử dụng.");
        if (challenge.ExpiresAtUtc <= DateTime.UtcNow) return new(false, "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");
        var maxOtpAttempts=await db.OperationalRuntimePolicies.AsNoTracking().Select(x=>x.OtpMaxAttempts).SingleAsync(cancellationToken);
        if (challenge.AttemptCount >= maxOtpAttempts) return new(false, "Mã OTP đã bị khóa do nhập sai quá nhiều lần.");

        if (!VerifyOtp(challenge, code))
        {
            await db.PayoutOtpChallenges.Where(x => x.Id == challengeId && x.ConsumedAtUtc == null && x.AttemptCount < maxOtpAttempts)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), cancellationToken);
            return new(false, "Mã OTP không đúng.");
        }

        if (!challenge.PayoutAccountId.HasValue) return new(false, "Yêu cầu rút tiền chưa có tài khoản nhận.");
        return await CreatePayoutAsync(kocUserId, challenge.PayoutAccountId.Value, challenge.Amount, cancellationToken, challengeId, maxOtpAttempts);
    }

    private async Task<WalletResult> CreatePayoutAsync(Guid kocUserId, Guid payoutAccountId, decimal amount, CancellationToken cancellationToken, Guid? challengeId = null, int maxOtpAttempts = PayoutOtpRules.MaxAttempts)
    {
        if (!WalletRules.IsValidAmount(amount)) return new(false, "Số tiền rút phải là số nguyên đồng hợp lệ và lớn hơn 0.");
        var kocId = await db.KocProfiles.AsNoTracking().Where(x => x.UserId == kocUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!kocId.HasValue) return new(false, "Không tìm thấy hồ sơ KOC.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var policy=await db.PayoutPolicies.AsNoTracking().SingleOrDefaultAsync(x=>x.Code=="DEFAULT",cancellationToken);if(policy is null)return new(false,"Hệ thống chưa cấu hình payout policy.");if(amount<policy.MinimumAmount)return new(false,$"Số tiền rút tối thiểu là {policy.MinimumAmount:N0} ₫.");
        var vietnamNow=DateTime.UtcNow.AddHours(7);var dayStartUtc=vietnamNow.Date.AddHours(-7);var dayEndUtc=dayStartUtc.AddDays(1);var todayCount=await db.PayoutRequests.CountAsync(x=>x.KocProfileId==kocId.Value&&x.RequestedAtUtc>=dayStartUtc&&x.RequestedAtUtc<dayEndUtc,cancellationToken);if(todayCount>=policy.DailyRequestLimit)return new(false,$"Bạn đã đạt giới hạn {policy.DailyRequestLimit} yêu cầu rút trong ngày.");
        var account = await db.KocPayoutAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == payoutAccountId && x.KocProfileId == kocId.Value && x.DeletedAtUtc == null, cancellationToken);
        if (account is null) return new(false, "Vui lòng thiết lập tài khoản nhận tiền trước.");
        var grossAvailable = await db.WalletTransactions.Where(x => x.KocProfileId == kocId.Value && x.Status == WalletStatuses.Available).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var reserved = await db.PayoutRequests.Where(x => x.KocProfileId == kocId.Value && (x.Status == PayoutStatuses.WithdrawRequested || x.Status == PayoutStatuses.PaidOut)).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        if (amount > grossAvailable - reserved) return new(false, $"Số dư khả dụng không đủ. Hiện còn {(grossAvailable - reserved):N0} ₫.");
        var now = DateTime.UtcNow;var fee=PayoutPolicyRules.CalculateFee(amount,policy.FixedFee,policy.FeeRate);if(fee>=amount)return new(false,"Phí rút tiền phải nhỏ hơn số tiền yêu cầu.");var net=amount-fee;
        if (challengeId.HasValue)
        {
            var consumed = await db.PayoutOtpChallenges
                .Where(x => x.Id == challengeId.Value && x.KocProfileId == kocId.Value && x.ConsumedAtUtc == null
                    && x.AttemptCount < maxOtpAttempts && x.ExpiresAtUtc > now && x.Amount == amount
                    && x.PayoutAccountId == payoutAccountId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
            if (consumed != 1) return new(false, "Mã OTP không còn hiệu lực hoặc thông tin rút tiền đã thay đổi.");
        }
        var payout = new PayoutRequestEntity { Id = Guid.NewGuid(), KocProfileId = kocId.Value, Amount = amount,FeeAmountSnapshot=fee,NetAmountSnapshot=net, Status = PayoutStatuses.WithdrawRequested, BankCode = account.BankCode, AccountHolderName = account.AccountHolderName, AccountNumberCiphertext = account.AccountNumberCiphertext, AccountNumberLast4 = account.AccountNumberLast4, RequestedAtUtc = now, CreatedAtUtc = now };
        db.PayoutRequests.Add(payout);
        db.PayoutRequestStatusHistories.Add(new PayoutRequestStatusHistoryEntity { Id = Guid.NewGuid(), PayoutRequestId = payout.Id, FromStatus = null, ToStatus = PayoutStatuses.WithdrawRequested, Note = $"Yêu cầu rút {amount:N0} ₫", ActorUserId = kocUserId, CreatedAtUtc = now });
        Audit(kocUserId, payout.Id, "RequestPayout", new { Amount = amount,Fee=fee,Net=net, account.BankCode, account.AccountNumberLast4 }, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, "Số dư vừa thay đổi bởi một yêu cầu khác. Vui lòng tải lại Wallet.");
        }
        return new(true, $"Đã gửi yêu cầu rút {amount:N0} ₫; phí {fee:N0} ₫; thực nhận {net:N0} ₫.", payout.Id);
    }

    private string ProtectOtp(Guid challengeId, string code)
        => payoutOtpProtector.Protect($"{challengeId:N}:{code}");

    private bool VerifyOtp(PayoutOtpChallengeEntity challenge, string code)
    {
        try
        {
            var expected = System.Text.Encoding.UTF8.GetBytes($"{challenge.Id:N}:{code}");
            var actual = System.Text.Encoding.UTF8.GetBytes(payoutOtpProtector.Unprotect(challenge.CodeHash));
            return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<AdminPayoutItem>> GetAdminPayoutsAsync(CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var rows = await (from payout in db.PayoutRequests.AsNoTracking()
                          join koc in db.KocProfiles.AsNoTracking() on payout.KocProfileId equals koc.Id
                          orderby payout.Status == PayoutStatuses.WithdrawRequested ? 0 : 1, payout.RequestedAtUtc descending
                          select new { Payout = payout, koc.DisplayName }).ToWindowAsync(window, cancellationToken, q => q.Where(x => (window!.Search == null || x.DisplayName.Contains(window.Search) || x.Payout.BankCode.Contains(window.Search) || (x.Payout.PayoutReference != null && x.Payout.PayoutReference.Contains(window.Search))) && (window.Status == null || x.Payout.Status == window.Status)), x => x.Payout.Status);
        var ids = rows.Select(x => x.Payout.Id).ToArray();
        var histories = await db.PayoutRequestStatusHistories.AsNoTracking().Where(x => ids.Contains(x.PayoutRequestId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(x => new AdminPayoutItem(x.Payout.Id, x.DisplayName, x.Payout.Amount,x.Payout.FeeAmountSnapshot,x.Payout.NetAmountSnapshot, x.Payout.Status, x.Payout.BankCode, x.Payout.AccountHolderName,
            TryUnprotect(x.Payout.AccountNumberCiphertext), x.Payout.PayoutReference, x.Payout.FailureReason, x.Payout.RequestedAtUtc)
        { History = histories.Where(h => h.PayoutRequestId == x.Payout.Id).Select(h => new StatusHistoryItem(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList() }).ToList();
    }

    public async Task<WalletResult> CancelPayoutAsync(Guid kocUserId, Guid payoutRequestId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            return new(false, "Vui lòng nhập lý do hủy từ 10 đến 500 ký tự.");

        var payout = await (from request in db.PayoutRequests.AsNoTracking()
                            join koc in db.KocProfiles.AsNoTracking() on request.KocProfileId equals koc.Id
                            where request.Id == payoutRequestId && koc.UserId == kocUserId
                            select request).FirstOrDefaultAsync(cancellationToken);
        if (payout is null)
            return new(false, "Không tìm thấy yêu cầu rút tiền.");
        if (!PayoutRules.CanCancel(payout.Status))
            return new(false, "Chỉ có thể hủy yêu cầu đang chờ xử lý.");

        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        int affected;
        try
        {
            affected = await db.PayoutRequests
                .Where(x => x.Id == payoutRequestId
                            && x.KocProfileId == payout.KocProfileId
                            && x.Status == PayoutStatuses.WithdrawRequested)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, PayoutStatuses.Cancelled)
                    .SetProperty(x => x.FailureReason, reason)
                    .SetProperty(x => x.ProcessedByUserId, kocUserId)
                    .SetProperty(x => x.ProcessedAtUtc, now)
                    .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        }
        catch (DbException exception) when (
            exception.Message.Contains("CK_PayoutRequests_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new(false,
                "Cấu hình trạng thái rút tiền của hệ thống chưa được cập nhật. Vui lòng chạy SQL migration 100 và thử lại.");
        }
        if (affected != 1)
            return new(false, "Yêu cầu vừa được xử lý ở phiên khác. Vui lòng tải lại.");

        db.PayoutRequestStatusHistories.Add(new PayoutRequestStatusHistoryEntity
        {
            Id = Guid.NewGuid(),
            PayoutRequestId = payout.Id,
            FromStatus = PayoutStatuses.WithdrawRequested,
            ToStatus = PayoutStatuses.Cancelled,
            Note = reason,
            ActorUserId = kocUserId,
            CreatedAtUtc = now
        });
        Audit(kocUserId, payout.Id, "CancelPayout", new
        {
            Before = payout.Status,
            After = PayoutStatuses.Cancelled,
            payout.Amount,
            Reason = reason
        }, now);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is DbException databaseException
            && databaseException.Message.Contains(
                "CK_PayoutRequestStatusHistories_",
                StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return new(false,
                "Cấu hình lịch sử trạng thái rút tiền chưa được cập nhật. Vui lòng chạy lại SQL migration 100 và thử lại.");
        }
        await transaction.CommitAsync(cancellationToken);
        return new(true, $"Đã hủy yêu cầu rút {payout.Amount:N0} ₫. Số tiền đã được hoàn lại số dư khả dụng.", payout.Id);
    }

    public async Task<WalletResult> ProcessPayoutAsync(Guid adminUserId, Guid payoutRequestId, string targetStatus, string? referenceOrReason, CancellationToken cancellationToken = default)
    {
        referenceOrReason = string.IsNullOrWhiteSpace(referenceOrReason) ? null : referenceOrReason.Trim();
        var payout = await db.PayoutRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == payoutRequestId, cancellationToken);
        if (payout is null) return new(false, "Không tìm thấy yêu cầu payout.");
        if (!PayoutRules.CanProcess(payout.Status, targetStatus)) return new(false, "Yêu cầu payout không còn ở trạng thái có thể xử lý.");
        if (referenceOrReason is null || referenceOrReason.Length > 200) return new(false, targetStatus == PayoutStatuses.PaidOut ? "Vui lòng nhập mã tham chiếu chuyển khoản." : "Vui lòng nhập lý do thất bại.");
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.PayoutRequests.Where(x => x.Id == payoutRequestId && x.Status == PayoutStatuses.WithdrawRequested)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, targetStatus)
                .SetProperty(x => x.PayoutReference, targetStatus == PayoutStatuses.PaidOut ? referenceOrReason : null)
                .SetProperty(x => x.FailureReason, targetStatus == PayoutStatuses.Failed ? referenceOrReason : null)
                .SetProperty(x => x.ProcessedByUserId, adminUserId).SetProperty(x => x.ProcessedAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        if (affected != 1) return new(false, "Yêu cầu vừa được xử lý bởi phiên khác. Vui lòng tải lại.");
        db.PayoutRequestStatusHistories.Add(new PayoutRequestStatusHistoryEntity { Id = Guid.NewGuid(), PayoutRequestId = payout.Id, FromStatus = PayoutStatuses.WithdrawRequested, ToStatus = targetStatus, Note = referenceOrReason, ActorUserId = adminUserId, CreatedAtUtc = now });
        var kocUserId = await db.KocProfiles.AsNoTracking().Where(x => x.Id == payout.KocProfileId).Select(x => x.UserId).FirstAsync(cancellationToken);
        Audit(adminUserId, payout.Id, "ProcessPayout", new { Before = payout.Status, After = targetStatus, Amount = payout.Amount, ReferenceOrReason = referenceOrReason }, now);
        Notify(kocUserId, "Payout", targetStatus == PayoutStatuses.PaidOut ? "Yêu cầu rút tiền đã chi trả" : "Yêu cầu rút tiền thất bại", targetStatus == PayoutStatuses.PaidOut ? $"Đã chi trả {payout.NetAmountSnapshot:N0} ₫ sau phí. Mã tham chiếu: {referenceOrReason}" : $"Yêu cầu {payout.Amount:N0} ₫ thất bại: {referenceOrReason}", "PayoutRequest", payout.Id, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, targetStatus == PayoutStatuses.PaidOut ? "Đã xác nhận chi trả." : "Đã đánh dấu payout thất bại.", payout.Id);
    }

    public async Task<PayoutPolicyItem> GetPayoutPolicyAsync(CancellationToken cancellationToken = default)
    {
        var policy = await db.PayoutPolicies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == "DEFAULT", cancellationToken);
        if (policy is null)
            throw new InvalidOperationException("Chưa cấu hình chính sách rút tiền trong Admin.");

        var history = await db.PayoutPolicyHistories.AsNoTracking()
            .Where(x => x.PolicyId == policy.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new PayoutPolicyHistoryItem(
                x.MinimumAmount, x.DailyRequestLimit, x.FixedFee, x.FeeRate, x.Reason, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new(policy.MinimumAmount, policy.DailyRequestLimit, policy.FixedFee, policy.FeeRate, policy.EffectiveAtUtc, history);
    }

    public async Task<WalletResult> UpdatePayoutPolicyAsync(Guid adminUserId, decimal minimumAmount, int dailyRequestLimit, decimal fixedFee, decimal feeRate, string reason, CancellationToken cancellationToken = default)
    {
        var error = PayoutPolicyRules.Validate(minimumAmount, dailyRequestLimit, fixedFee, feeRate);
        if (error is not null) return new(false, error);
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 5 or > 500) return new(false, "Lý do thay đổi phải từ 5 đến 500 ký tự.");
        if (PayoutPolicyRules.CalculateFee(minimumAmount, fixedFee, feeRate) >= minimumAmount)
            return new(false, "Phí phải nhỏ hơn số tiền rút tối thiểu.");

        var policy = await db.PayoutPolicies.SingleOrDefaultAsync(x => x.Code == "DEFAULT", cancellationToken);
        if (policy is null) return new(false, "Chưa khởi tạo chính sách rút tiền. Vui lòng chạy migration SQL mới nhất.");
        if (policy.MinimumAmount == minimumAmount && policy.DailyRequestLimit == dailyRequestLimit
            && policy.FixedFee == fixedFee && policy.FeeRate == feeRate)
            return new(false, "Chính sách không có thay đổi.");

        var now = DateTime.UtcNow;
        policy.MinimumAmount = minimumAmount;
        policy.DailyRequestLimit = dailyRequestLimit;
        policy.FixedFee = fixedFee;
        policy.FeeRate = feeRate;
        policy.EffectiveAtUtc = now;
        policy.UpdatedByUserId = adminUserId;
        policy.UpdatedAtUtc = now;
        db.PayoutPolicyHistories.Add(new PayoutPolicyHistoryEntity
        {
            Id = Guid.NewGuid(), PolicyId = policy.Id, MinimumAmount = minimumAmount,
            DailyRequestLimit = dailyRequestLimit, FixedFee = fixedFee, FeeRate = feeRate,
            Reason = reason, ActorUserId = adminUserId, CreatedAtUtc = now
        });
        Audit(adminUserId, policy.Id, "UpdatePayoutPolicy", new { minimumAmount, dailyRequestLimit, fixedFee, feeRate, reason }, now);
        await db.SaveChangesAsync(cancellationToken);
        return new(true, "Đã cập nhật chính sách và lưu lịch sử thay đổi.", policy.Id);
    }

    private string TryUnprotect(string ciphertext)
    {
        try { return payoutProtector.Unprotect(ciphertext); }
        catch (CryptographicException) { return "[Không thể giải mã]"; }
    }

    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now) => db.AuditLogs.Add(new AuditLogEntity { ActorUserId = actor, EntityName = "WalletTransaction", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });
    private void Notify(Guid userId, string type, string title, string body, string sourceType, Guid sourceId, DateTime now) => db.Notifications.Add(NotificationFactory.Create(userId,type,title,body,now,sourceType,sourceId,audienceRole:"KOC"));
}
