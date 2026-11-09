using System.Data;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Topstar.Application.Billing;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Payments;

public sealed record VnPayResult(bool Success, string Message, string RspCode = "99", string? RedirectUrl = null)
{
    public decimal? Amount { get; init; }
    public string? OrderNumber { get; init; }
    public string? TransactionNo { get; init; }
    public string? TargetUrl { get; init; }
    public string? TargetLabel { get; init; }
}

public sealed class VnPayService(TopstarDbContext db, IDataProtectionProvider protection,VnPayCallbackJournal journal)
{
    private string Secret(PaymentMethodConfigurationEntity c) =>
        protection.CreateProtector("Topstar.VnPay.HashSecret.v1").Unprotect(c.VnPayHashSecretCiphertext!);

    public async Task<VnPayResult> CreateAsync(Guid brandUserId, Guid targetId, bool isFunding, string clientIp, CancellationToken ct)
    {
        var config = await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is null || !config.VnPayEnabled || string.IsNullOrWhiteSpace(config.VnPayTmnCode) || string.IsNullOrWhiteSpace(config.VnPayHashSecretCiphertext))
            return new(false, "Thanh toán online chưa được cấu hình đầy đủ.");
        if(!Uri.TryCreate(config.VnPayBaseUrl,UriKind.Absolute,out var paymentUri) || paymentUri.Scheme!="https"
            || !Uri.TryCreate(config.VnPayReturnUrl,UriKind.Absolute,out var returnUri)
            || !(returnUri.Scheme=="https" || config.VnPayEnvironment=="Test" && returnUri.Scheme=="http" && returnUri.IsLoopback)
            || !string.IsNullOrEmpty(returnUri.UserInfo))
            return new(false,"Địa chỉ cổng thanh toán chưa được cấu hình hợp lệ.");
        string secret;
        try{secret=Secret(config);}
        catch(System.Security.Cryptography.CryptographicException){return new(false,"Không đọc được khóa thanh toán. Vui lòng liên hệ quản trị để kiểm tra cấu hình thanh toán online.");}
        decimal amount;
        if (isFunding)
        {
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            var f=await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={targetId}").SingleOrDefaultAsync(ct);
            if(f is null||!await(from c in db.Campaigns join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id==f.CampaignId&&b.UserId==brandUserId select c.Id).AnyAsync(ct))
                return new(false,"Không tìm thấy yêu cầu thanh toán.");
            if (f is null || f.Status != CampaignFundingStatuses.Pending || f.PaymentMethod != "VnPay") return new(false, "Yêu cầu ký quỹ không còn chờ thanh toán online.");
            if(!await db.Campaigns.AnyAsync(c=>c.Id==f.CampaignId&&(c.Status=="Published"||c.Status=="InProgress"||(c.CheckoutAtCreation&&c.Status=="Draft")),ct))return new(false,"Chiến dịch đã ngừng hoặc kết thúc; không thể bắt đầu thanh toán mới.");
            amount = f.TotalPayableSnapshot-f.BalanceAppliedAmount;
            f.OnlinePaymentStartedAtUtc??=DateTime.UtcNow;
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        }
        else
        {
            var p = await (from p0 in db.BrandPackagePurchases.AsNoTracking() join b in db.BrandProfiles on p0.BrandProfileId equals b.Id
                where p0.Id == targetId && b.UserId == brandUserId select p0).FirstOrDefaultAsync(ct);
            if (p is null || p.Status != BrandPackagePurchaseStatuses.Pending || p.PaymentMethod != "VnPay") return new(false, "Đơn mua gói không còn chờ thanh toán online.");
            amount = p.AmountSnapshot;
        }
        if (amount <= 0 || decimal.Truncate(amount) != amount) return new(false, "Số tiền thanh toán không hợp lệ.");
        var reference = (isFunding ? "F" : "P") + targetId.ToString("N");
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
        var ip = IPAddress.TryParse(clientIp, out var parsed) && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? parsed.ToString() : "127.0.0.1";
        var values = new Dictionary<string,string>
        {
            ["vnp_Version"]="2.1.0", ["vnp_Command"]="pay", ["vnp_TmnCode"]=config.VnPayTmnCode,
            ["vnp_Amount"]=(amount*100).ToString("0",CultureInfo.InvariantCulture), ["vnp_CurrCode"]="VND",
            ["vnp_TxnRef"]=reference, ["vnp_OrderInfo"]=(isFunding?"Ky quy ":"Mua goi ")+reference,
            ["vnp_OrderType"]="other", ["vnp_Locale"]="vn", ["vnp_ReturnUrl"]=config.VnPayReturnUrl,
            ["vnp_IpAddr"]=ip, ["vnp_CreateDate"]=now.ToString("yyyyMMddHHmmss"), ["vnp_ExpireDate"]=now.AddMinutes(15).ToString("yyyyMMddHHmmss")
        };
        var url=config.VnPayBaseUrl+"?"+VnPayProtocol.Canonical(values)+"&vnp_SecureHash="+VnPayProtocol.Sign(values,secret);
        return new(true,"Đang chuyển đến cổng thanh toán online.","00",url);
    }

    // Return and IPN share signature verification and one atomic, idempotent settlement.
    public async Task<VnPayResult> ProcessAsync(IReadOnlyDictionary<string,StringValues> query, bool ipn, CancellationToken ct)
    {
        var logId=await journal.StartAsync(query,ipn);
        var signatureValid=false;
        try
        {
            var result=await ProcessCoreAsync(query,ipn,()=>signatureValid=true,ct);
            await journal.FinishAsync(logId,signatureValid,result);
            return result;
        }
        catch(Exception ex)
        {
            await journal.FinishAsync(logId,signatureValid,null,ex);
            throw;
        }
    }

    private async Task<VnPayResult> ProcessCoreAsync(IReadOnlyDictionary<string,StringValues> query,bool ipn,Action markSignatureValid,CancellationToken ct)
    {
        if (query.Any(x=>x.Key.StartsWith("vnp_",StringComparison.Ordinal) && x.Value.Count!=1)) return new(false,"Phản hồi thanh toán không hợp lệ.","97");
        var values=query.ToDictionary(x=>x.Key,x=>x.Value.ToString(),StringComparer.Ordinal);
        var config=await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct);
        if(config is null || string.IsNullOrWhiteSpace(config.VnPayHashSecretCiphertext) || !VnPayProtocol.Verify(values,Secret(config)))
            return new(false,"Chữ ký thanh toán không hợp lệ.","97");
        markSignatureValid();
        string V(string key)=>values.GetValueOrDefault(key)??"";
        if(V("vnp_TmnCode")!=config.VnPayTmnCode) return new(false,"Mã đơn vị thanh toán không hợp lệ.","97");
        var reference=V("vnp_TxnRef");
        if(reference.Length!=33 || reference[0] is not ('F' or 'P') || !Guid.TryParseExact(reference[1..],"N",out var targetId))
            return new(false,"Không tìm thấy yêu cầu thanh toán.","01");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var funding=reference[0]=='F'
            ? await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH (UPDLOCK,HOLDLOCK) WHERE Id={targetId}").FirstOrDefaultAsync(ct):null;
        var purchase=reference[0]=='P'
            ? await db.BrandPackagePurchases.FromSqlInterpolated($"SELECT * FROM dbo.BrandPackagePurchases WITH (UPDLOCK,HOLDLOCK) WHERE Id={targetId}").FirstOrDefaultAsync(ct):null;
        if(funding is null && purchase is null) return new(false,"Không tìm thấy yêu cầu thanh toán.","01");
        var amount=funding is not null?funding.TotalPayableSnapshot-funding.BalanceAppliedAmount:purchase!.AmountSnapshot;
        if(!VnPayProtocol.AmountMatches(V("vnp_Amount"),amount)) return new(false,"Số tiền thanh toán không khớp đơn hàng.","04");
        if((funding?.PaymentMethod??purchase!.PaymentMethod)!="VnPay") return new(false,"Phương thức thanh toán không khớp.","01");
        var receiptCount=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.VnPayReceipts WHERE TargetReference={reference}").SingleAsync(ct);
        VnPayResult PaidResult(string code)=>new(true,
            funding is not null?"Khoản ký quỹ đã được ghi nhận. Bạn có thể tiếp tục quản lý chiến dịch.":"Gói dịch vụ đã được kích hoạt. Bạn có thể sử dụng gói để tạo chiến dịch.",
            code)
        {
            Amount=amount,OrderNumber=funding?.OrderNumber??purchase!.OrderNumber,
            TransactionNo=funding?.PaymentReference??purchase!.PaymentReference,
            TargetUrl=funding is not null?"/Brand/CampaignFunding?campaignId="+funding.CampaignId:"/Brand/Packages",
            TargetLabel=funding is not null?"Về ký quỹ chiến dịch":"Sử dụng gói dịch vụ"
        };
        if(receiptCount>0) return PaidResult("02");
        if((funding?.Status??purchase!.Status)!="Pending") return new(false,"Yêu cầu không còn ở trạng thái chờ thanh toán.","02");
        if(V("vnp_ResponseCode")!="00" || V("vnp_TransactionStatus")!="00")
            return new(false,"Giao dịch chưa thành công. Bạn có thể thử thanh toán lại.","00");
        var transactionNo=V("vnp_TransactionNo");
        if(transactionNo.Length is <1 or >100 || !transactionNo.All(char.IsAsciiDigit) || transactionNo=="0") return new(false,"Mã giao dịch không hợp lệ.","97");
        if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.VnPayReceipts WHERE TransactionNo={transactionNo}").SingleAsync(ct)>0)
            return new(false,"Mã giao dịch đã được dùng cho đơn khác.","99");
        var now=DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.VnPayReceipts(TransactionNo,TargetReference,Amount,CreatedAtUtc) VALUES({transactionNo},{reference},{amount},{now})",ct);
        Guid brandId;
        if(funding is not null)
        {
            funding.Status=CampaignFundingStatuses.Paid;funding.PaidAtUtc=now;funding.PaymentReference=transactionNo;funding.UpdatedAtUtc=now;
            // Funding does not open KOC work. Brand still chooses when to launch.
            brandId=await db.Campaigns.Where(x=>x.Id==funding.CampaignId).Select(x=>x.BrandProfileId).SingleAsync(ct);
            db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=funding.Id,EntryType="PaidVnPay",Amount=amount,Note=transactionNo,CreatedAtUtc=now});
        }
        else
        {
            purchase!.Status=BrandPackagePurchaseStatuses.Paid;purchase.PaidAtUtc=now;purchase.PaymentReference=transactionNo;purchase.UpdatedAtUtc=now;
            purchase.ExpiresAtUtc=purchase.ValidityDaysSnapshot.HasValue?now.AddDays(purchase.ValidityDaysSnapshot.Value):null;
            brandId=purchase.BrandProfileId;
            db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=purchase.Id,FromStatus="Pending",ToStatus="Paid",Note="Thanh toán online VNPay: "+transactionNo,CreatedAtUtc=now});
        }
        var userId=await db.BrandProfiles.Where(x=>x.Id==brandId).Select(x=>x.UserId).SingleAsync(ct);
        db.Notifications.Add(new NotificationEntity{Id=Guid.NewGuid(),UserId=userId,Type="Payment",Title="Thanh toán online thành công",
            Body=$"Đã ghi nhận thanh toán {amount:N0} đ cho {(funding is not null?"yêu cầu ký quỹ":"đơn mua gói")}.",CreatedAtUtc=now,
            SourceEntityType=funding is not null?"CampaignFunding":"BrandPackagePurchase",SourceEntityId=targetId,AudienceRole="BRAND",IsVisible=true,
            TargetUrl=funding is not null?"/Brand/CampaignFunding?campaignId="+funding.CampaignId:"/Brand/PackageCheckout?purchaseId="+targetId});
        db.AuditLogs.Add(new(){EntityName=funding is not null?"CampaignFunding":"BrandPackagePurchase",EntityId=targetId.ToString(),ActionName="VnPayPaid",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{reference,transactionNo,amount,Source=ipn?"IPN":"VerifiedReturn"}),CreatedAtUtc=now});
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return PaidResult("00");
    }
}
