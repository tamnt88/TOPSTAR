using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Matching;

public sealed class BrandPackagePurchaseService(TopstarDbContext db) : IBrandPackagePurchaseService
{
    public async Task<IReadOnlyList<BrandPackagePurchaseItem>> GetForBrandAsync(Guid userId, CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var query = (from p in db.BrandPackagePurchases.AsNoTracking()
            join b in db.BrandProfiles.AsNoTracking() on p.BrandProfileId equals b.Id
            join c0 in db.Campaigns.AsNoTracking() on p.CampaignId equals c0.Id into campaigns
            from c in campaigns.DefaultIfEmpty()
            where b.UserId == userId
            orderby p.PurchasedAtUtc descending
            select new { Purchase=p, BrandName=b.CompanyName, CampaignTitle=c == null ? null : c.Title });
        if(window is not null) {
            window.Amounts=await query.GroupBy(x=>x.Purchase.Status).Select(g=>new{Status=g.Key,Amount=g.Sum(x=>x.Purchase.AmountSnapshot)}).ToDictionaryAsync(x=>x.Status,x=>x.Amount,ct);
            window.AvailableCount=await query.CountAsync(x=>x.Purchase.Status==BrandPackagePurchaseStatuses.Paid&&x.Purchase.CampaignId==null&&(!x.Purchase.ExpiresAtUtc.HasValue||x.Purchase.ExpiresAtUtc>DateTime.UtcNow),ct);
        }
        var rows=await query.ToWindowAsync(window,ct,q=>q.Where(x=>(window!.Search==null||x.Purchase.PackageNameSnapshot.Contains(window.Search)||x.Purchase.OrderNumber.Contains(window.Search)||x.BrandName.Contains(window.Search))&&(window.Status==null||x.Purchase.Status==window.Status)),x=>x.Purchase.Status);
        return rows.Select(x => Map(x.Purchase, x.CampaignTitle) with { BrandName=x.BrandName,VatAmount=x.Purchase.VatAmountSnapshot,VatAdded=x.Purchase.VatAddedSnapshot,VatMode=x.Purchase.VatModeSnapshot,VatRate=x.Purchase.VatRateSnapshot }).ToList();
    }

    public async Task<IReadOnlyList<BrandPackagePurchaseItem>> GetForAdminAsync(CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var query = (from p in db.BrandPackagePurchases.AsNoTracking()
            join b in db.BrandProfiles.AsNoTracking() on p.BrandProfileId equals b.Id
            join c0 in db.Campaigns.AsNoTracking() on p.CampaignId equals c0.Id into campaigns
            from c in campaigns.DefaultIfEmpty()
            orderby p.PurchasedAtUtc descending
            select new { Purchase=p, BrandName=b.CompanyName, CampaignTitle=c == null ? null : c.Title });
        if(window is not null) {
            window.Amounts=await query.GroupBy(x=>x.Purchase.Status).Select(g=>new{Status=g.Key,Amount=g.Sum(x=>x.Purchase.AmountSnapshot)}).ToDictionaryAsync(x=>x.Status,x=>x.Amount,ct);
            window.AvailableCount=await query.CountAsync(x=>x.Purchase.Status==BrandPackagePurchaseStatuses.Paid&&x.Purchase.CampaignId==null&&(!x.Purchase.ExpiresAtUtc.HasValue||x.Purchase.ExpiresAtUtc>DateTime.UtcNow),ct);
        }
        var rows=await query.ToWindowAsync(window,ct,q=>q.Where(x=>(window!.Search==null||x.Purchase.PackageNameSnapshot.Contains(window.Search)||x.Purchase.OrderNumber.Contains(window.Search)||x.BrandName.Contains(window.Search))&&(window.Status==null||x.Purchase.Status==window.Status)),x=>x.Purchase.Status);
        var ids=rows.Select(x=>x.Purchase.Id).ToList();
        var histories=await db.BrandPackagePurchaseHistories.AsNoTracking().Where(h=>ids.Contains(h.PurchaseId)&&h.ToStatus=="CancelRequested").OrderByDescending(h=>h.CreatedAtUtc).ToListAsync(ct);
        return rows.Select(x => Map(x.Purchase, x.CampaignTitle) with { BrandName=x.BrandName,CancellationReason=histories.FirstOrDefault(h=>h.PurchaseId==x.Purchase.Id)?.Note }).ToList();
    }

    public Task<BrandPackagePurchaseItem?> GetUsableAsync(Guid userId, Guid purchaseId, CancellationToken ct = default)
    {
        return LoadUsableAsync(userId, purchaseId, ct);
    }

    public async Task<BrandPackagePurchaseResult> CreateOrderAsync(Guid userId, Guid policyId, string paymentMethod = "BankTransfer", CancellationToken ct = default)
    {
        if(paymentMethod is not ("BankTransfer" or "OnePay" or "VnPay"))return new(false,"Phương thức thanh toán không hợp lệ.");
        var config=await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct);
        if(config is null || paymentMethod=="BankTransfer"&&!config.BankTransferEnabled || paymentMethod=="OnePay"&&!config.OnePayEnabled || paymentMethod=="VnPay"&&!config.VnPayEnabled)
            return new(false,"Phương thức thanh toán đang tạm khóa.");
        var brandId = await db.BrandProfiles.Where(x => x.UserId == userId && x.Status == "Approved")
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!brandId.HasValue) return new(false, "Hồ sơ Brand chưa được duyệt.");
        var policy = await db.CampaignPackagePolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == policyId && x.IsActive && !x.IsDeleted && x.SalePrice > 0, ct);
        if (policy is null) return new(false, "Gói KOC không còn khả dụng.");
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var quote=await Topstar.Infrastructure.Finance.BrandTaxPricing.QuoteAsync(db,"Package",policy.SalePrice,policy.SalePrice,ct);
        var order = new BrandPackagePurchaseEntity
        {
            Id=id, BrandProfileId=brandId.Value, PackagePolicyId=policy.Id,
            OrderNumber=$"PKG-{now:yyyyMMdd}-{id.ToString("N")[..8].ToUpperInvariant()}",
            PackageCodeSnapshot=policy.Code, PackageNameSnapshot=policy.Name,
            AmountSnapshot=quote.Total,VatPolicyIdSnapshot=quote.PolicyId,VatModeSnapshot=quote.Mode,VatRateSnapshot=quote.Rate,VatAmountSnapshot=quote.Vat,VatAddedSnapshot=quote.AddedVat, ValidityDaysSnapshot=policy.ValidityDays,
            MinBudgetAmountSnapshot=policy.MinBudgetAmount, MaxBudgetAmountSnapshot=policy.MaxBudgetAmount,
            BrandSelectionEnabledSnapshot=policy.BrandSelectionEnabled, SelectionQuotaSnapshot=policy.SelectionQuota,
            Status=BrandPackagePurchaseStatuses.Pending, PaymentMethod=paymentMethod, PurchasedAtUtc=now, CreatedAtUtc=now
        };
        db.BrandPackagePurchases.Add(order);
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=id,ToStatus=order.Status,Note="Brand tạo đơn mua gói; chờ xác nhận thanh toán.",ActorUserId=userId,CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=userId,EntityName="BrandPackagePurchase",EntityId=id.ToString(),ActionName="CreateOrder",PayloadJson=JsonSerializer.Serialize(new{policy.Code,policy.SalePrice}),CreatedAtUtc=now});
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return new(false, "Chưa thể tạo đơn mua gói lúc này. Vui lòng thử lại hoặc liên hệ Topstar.");
        }
        return new(true, $"Đã tạo đơn {order.OrderNumber}. Vui lòng thanh toán và chờ Topstar xác nhận.", id);
    }

    public async Task<BrandPackagePurchaseResult> SubmitPaymentProofAsync(Guid userId, Guid purchaseId, string proofUrl, string note, CancellationToken ct = default)
    {
        note=(note??string.Empty).Trim();
        if(!proofUrl.StartsWith("/uploads/payment-proofs/",StringComparison.OrdinalIgnoreCase))return new(false,"Chứng từ thanh toán không hợp lệ.");
        if(note.Length is <10 or >500)return new(false,"Ghi chú chuyển khoản phải từ 10 đến 500 ký tự.");
        var purchase=await(from p in db.BrandPackagePurchases join b in db.BrandProfiles on p.BrandProfileId equals b.Id where p.Id==purchaseId&&b.UserId==userId select p).FirstOrDefaultAsync(ct);
        if(purchase is null)return new(false,"Không tìm thấy đơn mua gói hoặc doanh nghiệp không có quyền thao tác.");
        if(purchase.Status!=BrandPackagePurchaseStatuses.Pending||purchase.PaymentMethod!="BankTransfer")return new(false,"Đơn này không ở trạng thái có thể gửi chứng từ chuyển khoản.");
        var now=DateTime.UtcNow;purchase.PaymentProofUrl=proofUrl;purchase.PaymentProofNote=note;purchase.PaymentProofSubmittedAtUtc=now;purchase.UpdatedAtUtc=now;
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=purchase.Id,FromStatus=purchase.Status,ToStatus=purchase.Status,Note="Doanh nghiệp đã gửi chứng từ chuyển khoản.",ActorUserId=userId,CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=userId,EntityName="BrandPackagePurchase",EntityId=purchase.Id.ToString(),ActionName="SubmitPaymentProof",PayloadJson=JsonSerializer.Serialize(new{HasProof=true,NoteLength=note.Length}),CreatedAtUtc=now});
        await db.SaveChangesAsync(ct);return new(true,"Đã gửi chứng từ. Topstar sẽ kiểm tra và cập nhật trạng thái thanh toán.",purchase.Id);
    }

    public async Task<BrandPackagePurchaseResult> ConfirmPaidAsync(Guid adminUserId, Guid purchaseId, string paymentReference, CancellationToken ct = default)
    {
        paymentReference = paymentReference?.Trim() ?? string.Empty;
        if (paymentReference.Length is < 3 or > 100) return new(false, "Mã tham chiếu thanh toán phải từ 3 đến 100 ký tự.");
        var purchase = await db.BrandPackagePurchases.FirstOrDefaultAsync(x => x.Id == purchaseId, ct);
        if (purchase is null) return new(false, "Không tìm thấy đơn mua gói.");
        if (purchase.PaymentMethod=="VnPay") return new(false,"Đơn thanh toán online VNPay chỉ được xác nhận qua IPN hợp lệ.");
        if (purchase.Status != BrandPackagePurchaseStatuses.Pending) return new(false, "Chỉ đơn đang chờ thanh toán mới được xác nhận.");
        var now = DateTime.UtcNow;
        purchase.Status=BrandPackagePurchaseStatuses.Paid;
        purchase.PaymentReference=paymentReference;
        purchase.PaidAtUtc=now;
        purchase.ExpiresAtUtc=purchase.ValidityDaysSnapshot.HasValue ? now.AddDays(purchase.ValidityDaysSnapshot.Value) : null;
        purchase.UpdatedAtUtc=now;
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=purchase.Id,FromStatus=BrandPackagePurchaseStatuses.Pending,ToStatus=BrandPackagePurchaseStatuses.Paid,Note=$"Admin xác nhận thanh toán: {paymentReference}",ActorUserId=adminUserId,CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=adminUserId,EntityName="BrandPackagePurchase",EntityId=purchase.Id.ToString(),ActionName="ConfirmPaid",PayloadJson=JsonSerializer.Serialize(new{paymentReference}),CreatedAtUtc=now});
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(false, "Đơn mua vừa được xử lý ở một phiên khác. Vui lòng tải lại trang.");
        }
        catch (DbUpdateException)
        {
            return new(false, "Chưa thể xác nhận thanh toán lúc này. Vui lòng kiểm tra lại thông tin.");
        }
        return new(true, "Đã xác nhận thanh toán; Brand có thể dùng gói để tạo một chiến dịch.", purchase.Id);
    }

    public async Task<BrandPackagePurchaseResult> CancelPendingAsync(Guid userId, Guid purchaseId, string reason, CancellationToken ct = default)
    {
        reason=reason?.Trim()??string.Empty;
        if(reason.Length is <10 or >500)return new(false,"Lý do hủy phải từ 10 đến 500 ký tự.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        var purchase = await (from p in db.BrandPackagePurchases
            join b in db.BrandProfiles on p.BrandProfileId equals b.Id
            where p.Id == purchaseId && b.UserId == userId
            select p).FirstOrDefaultAsync(ct);
        if (purchase is null) return new(false, "Không tìm thấy đơn mua gói hoặc Brand không có quyền thao tác.");
        if (purchase.Status != BrandPackagePurchaseStatuses.Pending)
            return new(false, "Chỉ đơn đang chờ thanh toán mới có thể hủy.");
        var now=DateTime.UtcNow;
        purchase.Status=BrandPackagePurchaseStatuses.Cancelled;purchase.UpdatedAtUtc=now;
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=purchase.Id,FromStatus=BrandPackagePurchaseStatuses.Pending,ToStatus=BrandPackagePurchaseStatuses.Cancelled,Note=reason,ActorUserId=userId,CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=userId,EntityName="BrandPackagePurchase",EntityId=purchase.Id.ToString(),ActionName="CancelPending",PayloadJson=JsonSerializer.Serialize(new{purchase.OrderNumber,Reason=reason}),CreatedAtUtc=now});
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}
        catch(DbUpdateConcurrencyException){return new(false,"Đơn vừa được xử lý ở phiên khác. Vui lòng tải lại trang.");}
        return new(true,"Đã hủy đơn mua gói.",purchase.Id);
    }

    private async Task<BrandPackagePurchaseItem?> LoadUsableAsync(Guid userId, Guid purchaseId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var row = await (from p in db.BrandPackagePurchases.AsNoTracking()
            join b in db.BrandProfiles.AsNoTracking() on p.BrandProfileId equals b.Id
            where b.UserId == userId && p.Id == purchaseId && p.Status == BrandPackagePurchaseStatuses.Paid
                && p.CampaignId == null && (!p.ExpiresAtUtc.HasValue || p.ExpiresAtUtc > now)
            select p).FirstOrDefaultAsync(ct);
        return row is null ? null : Map(row, null);
    }

    private static BrandPackagePurchaseItem Map(BrandPackagePurchaseEntity p, string? campaignTitle)
        => new(p.Id,p.PackagePolicyId,p.OrderNumber,p.PackageCodeSnapshot,p.PackageNameSnapshot,p.AmountSnapshot,
            p.Status,p.PurchasedAtUtc,p.PaidAtUtc,p.ExpiresAtUtc,p.CampaignId,campaignTitle,p.PaymentReference,p.PaymentMethod,p.PaymentProofUrl,p.PaymentProofNote,p.PaymentProofSubmittedAtUtc)
        { VatAmount=p.VatAmountSnapshot,VatAdded=p.VatAddedSnapshot,VatMode=p.VatModeSnapshot,VatRate=p.VatRateSnapshot,MinBudgetAmount=p.MinBudgetAmountSnapshot, MaxBudgetAmount=p.MaxBudgetAmountSnapshot,
            BrandSelectionEnabled=p.BrandSelectionEnabledSnapshot, SelectionQuota=p.SelectionQuotaSnapshot };
}
