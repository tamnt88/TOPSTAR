using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Finance;

public sealed class BrandBalanceRow
{
 public Guid BrandProfileId{get;set;} public decimal Available{get;set;} public decimal Reserved{get;set;} public DateTime UpdatedAtUtc{get;set;}
}
public sealed class BrandBalancePolicy
{
 public int Id{get;set;} public bool WithdrawalsEnabled{get;set;} public decimal MinimumWithdrawal{get;set;}
 public decimal WithdrawalFee{get;set;} public decimal DualApprovalThreshold{get;set;} public int ProcessingDays{get;set;}
 public DateTime UpdatedAtUtc{get;set;} public Guid? UpdatedByUserId{get;set;}
}
public sealed class BrandWithdrawal
{
 public Guid Id{get;set;} public Guid BrandProfileId{get;set;} public Guid RequestKey{get;set;}
 public decimal Amount{get;set;} public decimal Fee{get;set;} public string BankName{get;set;}="";
 public string AccountNumber{get;set;}=""; public string AccountHolder{get;set;}=""; public string Status{get;set;}="";
 public bool DualApprovalRequired{get;set;} public Guid? FirstApprovedBy{get;set;} public Guid? ApprovedBy{get;set;}
 public string? PaymentReference{get;set;} public string? EvidenceUrl{get;set;} public string? Note{get;set;}
 public DateTime CreatedAtUtc{get;set;} public DateTime? UpdatedAtUtc{get;set;}
 [System.ComponentModel.DataAnnotations.Schema.NotMapped] public decimal NetAmount=>Amount-Fee;
}
public sealed class BrandBalanceEntry
{
 public Guid Id{get;set;} public Guid BrandProfileId{get;set;} public string EntryType{get;set;}="";
 public decimal Amount{get;set;} public decimal AvailableAfter{get;set;} public decimal ReservedAfter{get;set;}
 public Guid SourceId{get;set;} public Guid OperationKey{get;set;} public string Note{get;set;}="";
 public Guid? ActorUserId{get;set;} public DateTime CreatedAtUtc{get;set;}
}
public sealed record BrandBalanceResult(bool Success,string Message);
public sealed class BrandRefundSource
{
 public Guid Id{get;set;}public string BrandName{get;set;}="";public string CampaignTitle{get;set;}="";
 public string? OrderNumber{get;set;}public decimal Refundable{get;set;}
}
public sealed record BrandCreditApproval(Guid Operation,Guid FundingId,decimal Amount,Guid? FirstApprover);

public sealed class BrandBalanceService(TopstarDbContext db)
{
 public Task<BrandBalanceResult> CancelPackageAsync(Guid actor,Guid id,string reason,bool review,bool approve,CancellationToken ct)=>ExecuteAsync(async()=>
 {
  reason=(reason??"").Trim();
  if(reason.Length<10||reason.Length>500)return new(false,"Vui lòng nhập lý do từ 10 đến 500 ký tự.");
  if(!await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchases') AND name=N'CK_BrandPackagePurchases_Status' AND definition LIKE N'%CancelRequested%'").AnyAsync(x=>x>0,ct))return new(false,"Chức năng hủy gói cần triển khai SQL 164 trước khi sử dụng.");
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  var p=await db.BrandPackagePurchases.FromSqlInterpolated($"SELECT * FROM dbo.BrandPackagePurchases WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
  if(p is null)return new(false,"Không tìm thấy gói dịch vụ.");
  var brand=await db.BrandProfiles.SingleAsync(b=>b.Id==p.BrandProfileId,ct);
  if(review&&!await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id where ur.UserId==actor&&(role.Code=="Admin"||role.Code=="ADMIN") select ur.UserId).AnyAsync(ct))return new(false,"Bạn không có quyền xét duyệt.");
  if(!review&&brand.UserId!=actor)return new(false,"Bạn không có quyền hủy gói này.");
  if(p.CampaignId.HasValue)return new(false,"Gói đã gắn chiến dịch. Vui lòng xử lý dừng chiến dịch và đối soát với Admin.");
  var before=p.Status;
  if(review){
   if(before!="CancelRequested")return new(false,"Yêu cầu đã được xử lý hoặc không còn chờ duyệt.");
   if(approve){
    if(p.AmountSnapshot<=0||decimal.Truncate(p.AmountSnapshot)!=p.AmountSnapshot)return new(false,"Số tiền gói không hợp lệ.");
    var policy=await PolicyAsync(ct);
    if(p.AmountSnapshot>=policy.DualApprovalThreshold){
     var first=await db.AuditLogs.Where(a=>a.EntityName=="BrandBalance"&&a.EntityId==id.ToString()&&a.ActionName=="FirstPackageRefundApproval").OrderByDescending(a=>a.CreatedAtUtc).FirstOrDefaultAsync(ct);
     var lastRejection=await db.AuditLogs.Where(a=>a.EntityName=="BrandBalance"&&a.EntityId==id.ToString()&&a.ActionName=="ReviewPackageCancellation").OrderByDescending(a=>a.CreatedAtUtc).FirstOrDefaultAsync(ct);
     if(first is null||(lastRejection is not null&&lastRejection.CreatedAtUtc>=first.CreatedAtUtc)){
      Audit(actor,id,"FirstPackageRefundApproval",new{reason,p.AmountSnapshot});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã ghi nhận duyệt lần một. Cần quản trị viên khác duyệt để hoàn tiền.");
     }
     if(first.ActorUserId==actor)return new(false,"Khoản hoàn lớn cần quản trị viên thứ hai xét duyệt.");
    }
    var account=await LockAsync(db,brand.Id,ct);
    if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceLedgers WHERE SourceId={id} AND EntryType=N'PackageRefund'").SingleAsync(ct)>0)return new(false,"Gói đã được hoàn tiền.");
    await WriteAsync(db,account,p.AmountSnapshot,0,"PackageRefund",id,id,"Hoàn tiền hủy gói: "+p.PackageNameSnapshot,actor,ct);
    p.Status="Refunded";
   }else p.Status="Paid";
  }else{
   if(before!="Paid")return new(false,"Chỉ gói đã thanh toán và chưa sử dụng mới được yêu cầu hủy.");
   p.Status="CancelRequested";
  }
  p.UpdatedAtUtc=DateTime.UtcNow;
  var message=review?(approve?"Đã duyệt hủy và hoàn toàn bộ tiền gói vào số dư Brand.":"Yêu cầu hủy bị từ chối. Gói được mở lại theo hạn sử dụng."):"Đã gửi yêu cầu hủy gói. Gói tạm khóa trong lúc chờ Admin duyệt.";
  db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=id,FromStatus=before,ToStatus=p.Status,Note=reason,ActorUserId=actor,CreatedAtUtc=DateTime.UtcNow});
  Audit(actor,id,review?"ReviewPackageCancellation":"RequestPackageCancellation",new{before,p.Status,reason,p.AmountSnapshot});
  Notify(brand.UserId,"Cập nhật hủy gói dịch vụ",message,id);
  db.Notifications.Local.Last().TargetUrl="/Brand/Packages";
  if(!review){
   var admins=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Code=="Admin"||r.Code=="ADMIN" select ur.UserId).Distinct().ToListAsync(ct);
   foreach(var admin in admins)db.Notifications.Add(new(){Id=Guid.NewGuid(),UserId=admin,Type="Payment",Title="Yêu cầu hủy gói dịch vụ",Body=brand.CompanyName+" yêu cầu hủy gói "+p.PackageNameSnapshot,AudienceRole="ADMIN",IsVisible=true,SourceEntityType="BrandPackagePurchase",SourceEntityId=id,TargetUrl="/Admin/Finance/Billing",CreatedAtUtc=DateTime.UtcNow});
  }
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,message);
 });
 public Task<BrandBalanceResult> PurchasePackageAsync(Guid user,Guid policyId,Guid operation,CancellationToken ct)
    =>ExecuteAsync(async()=>
    {
        if(operation==Guid.Empty)return new(false,"Yêu cầu mua gói không hợp lệ.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var brand=await db.BrandProfiles.SingleOrDefaultAsync(b=>b.UserId==user&&b.Status=="Approved",ct);
        if(brand is null)return new(false,"Hồ sơ Brand chưa được duyệt.");
        var account=await LockAsync(db,brand.Id,ct);
        if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceLedgers WHERE BrandProfileId={brand.Id} AND OperationKey={operation}").SingleAsync(ct)>0)return new(true,"Đơn mua gói đã được thanh toán trước đó.");
        var policy=await db.CampaignPackagePolicies.SingleOrDefaultAsync(p=>p.Id==policyId&&p.IsActive&&!p.IsDeleted,ct);
        var quote=policy is null?null:await BrandTaxPricing.QuoteAsync(db,"Package",policy.SalePrice,policy.SalePrice,ct);
        if(policy is null||quote is null||!BrandBalanceRules.CanSpend(quote.Total,account.Available,quote.Total))return new(false,"Gói không còn khả dụng hoặc số dư không đủ thanh toán toàn bộ giá gói.");
        var now=DateTime.UtcNow;var id=Guid.NewGuid();var reference="BAL-"+operation.ToString("N");
        db.BrandPackagePurchases.Add(new(){Id=id,BrandProfileId=brand.Id,PackagePolicyId=policy.Id,OrderNumber=$"PKG-{now:yyyyMMdd}-{id.ToString("N")[..8].ToUpperInvariant()}",PackageCodeSnapshot=policy.Code,PackageNameSnapshot=policy.Name,AmountSnapshot=quote.Total,VatPolicyIdSnapshot=quote.PolicyId,VatModeSnapshot=quote.Mode,VatRateSnapshot=quote.Rate,VatAmountSnapshot=quote.Vat,VatAddedSnapshot=quote.AddedVat,ValidityDaysSnapshot=policy.ValidityDays,MinBudgetAmountSnapshot=policy.MinBudgetAmount,MaxBudgetAmountSnapshot=policy.MaxBudgetAmount,BrandSelectionEnabledSnapshot=policy.BrandSelectionEnabled,SelectionQuotaSnapshot=policy.SelectionQuota,Status="Paid",PaymentMethod="BrandBalance",PurchasedAtUtc=now,PaidAtUtc=now,ExpiresAtUtc=policy.ValidityDays.HasValue?now.AddDays(policy.ValidityDays.Value):null,PaymentReference=reference,CreatedAtUtc=now});
        await WriteAsync(db,account,-quote.Total,0,"PackagePayment",id,operation,"Thanh toán gói dịch vụ: "+policy.Name,user,ct);
        db.BrandPackagePurchaseHistories.Add(new(){Id=Guid.NewGuid(),PurchaseId=id,ToStatus="Paid",Note="Thanh toán đủ bằng số dư; gói được kích hoạt.",ActorUserId=user,CreatedAtUtc=now});
        Audit(user,id,"PurchasePackageBalance",new{policyId,amount=quote.Total,operation});
        Notify(user,"Mua gói thành công",$"Đã thanh toán {quote.Total:N0} đ bằng số dư. Gói {policy.Name} đã được kích hoạt.",id);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return new(true,"Đã thanh toán bằng số dư và kích hoạt gói dịch vụ.");
    });
 public Task<BrandBalanceResult> CreditRefundAsync(Guid actor,Guid fundingId,decimal amount,Guid operation,CancellationToken ct)
    => ExecuteAsync(()=>CreditRefundCoreAsync(actor,fundingId,amount,operation,ct));
 public Task<BrandBalanceResult> PayFundingAsync(Guid user,Guid fundingId,decimal amount,Guid operation,CancellationToken ct)
    => ExecuteAsync(()=>PayFundingCoreAsync(user,fundingId,amount,operation,ct));
 public Task<BrandBalanceResult> RequestWithdrawalAsync(Guid user,decimal amount,Guid operation,CancellationToken ct)
    => ExecuteAsync(()=>RequestWithdrawalCoreAsync(user,amount,operation,ct));
 public Task<BrandBalanceResult> ProcessWithdrawalAsync(Guid actor,Guid id,string action,string? reference,string? evidence,string? note,bool isAdmin,CancellationToken ct)
    => ExecuteAsync(()=>ProcessWithdrawalCoreAsync(actor,id,action,reference,evidence,note,isAdmin,ct));
 private async Task<BrandBalanceResult> ExecuteAsync(Func<Task<BrandBalanceResult>> action)
 {
    try{return await action();}
    catch(Exception ex) when(ex is Microsoft.Data.SqlClient.SqlException {Number:1205} || ex is DbUpdateException {InnerException:Microsoft.Data.SqlClient.SqlException {Number:1205}})
    {
        db.ChangeTracker.Clear();
        return new(false,"Có giao dịch khác đang xử lý cùng số dư. Thao tác này đã được hoàn tác; vui lòng tải lại và thử lại.");
    }
 }
 public IQueryable<BrandRefundSource> RefundSourcesQuery()=>db.Database.SqlQuery<BrandRefundSource>($"""
 SELECT f.Id,b.CompanyName AS BrandName,c.Title AS CampaignTitle,f.OrderNumber,
  CASE WHEN ISNULL(a.Refunds,0)+CASE WHEN c.Status='Cancelled' THEN f.CampaignBudgetSnapshot-ISNULL(a.Allocated,0) ELSE 0 END-f.RefundedAmount>0
   THEN ISNULL(a.Refunds,0)+CASE WHEN c.Status='Cancelled' THEN f.CampaignBudgetSnapshot-ISNULL(a.Allocated,0) ELSE 0 END-f.RefundedAmount ELSE 0 END AS Refundable
 FROM dbo.CampaignFundings f JOIN dbo.Campaigns c ON c.Id=f.CampaignId JOIN dbo.BrandProfiles b ON b.Id=c.BrandProfileId
 OUTER APPLY(SELECT SUM(Amount) Allocated,SUM(RefundedAmount) Refunds FROM dbo.CampaignFundingAllocations WHERE FundingId=f.Id) a
 WHERE f.Status IN('Paid','PartiallyReleased')
 """).Where(x=>x.Refundable>0);
 public Task<List<BrandRefundSource>> RefundSourcesAsync(CancellationToken ct)=>RefundSourcesQuery().ToListAsync(ct);
 public async Task<List<BrandCreditApproval>> CreditApprovalsAsync(CancellationToken ct)
 {
  var audits=await db.AuditLogs.FromSqlInterpolated($"SELECT a.* FROM dbo.AuditLogs a WHERE a.EntityName=N'BrandBalance' AND a.ActionName=N'ApproveRefundCredit' AND NOT EXISTS(SELECT 1 FROM dbo.BrandBalanceLedgers l WHERE a.EntityId=N'BrandBalanceCredit:'+REPLACE(CONVERT(nvarchar(36),l.OperationKey),N'-',N''))").AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct);
  var result=new List<BrandCreditApproval>();
  foreach(var audit in audits)
  {
   if(!Guid.TryParse(audit.EntityId?.Replace("BrandBalanceCredit:",""),out var operation))continue;
   if(string.IsNullOrWhiteSpace(audit.PayloadJson))continue;
   using var json=JsonDocument.Parse(audit.PayloadJson);
   result.Add(new(operation,json.RootElement.GetProperty("fundingId").GetGuid(),json.RootElement.GetProperty("amount").GetDecimal(),audit.ActorUserId));
  }
  return result;
 }
 public async Task<Guid?> BrandIdAsync(Guid user,CancellationToken ct)=>await db.BrandProfiles.Where(x=>x.UserId==user).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
 public async Task<BrandBalanceRow> GetAsync(Guid user,CancellationToken ct)
 {
  var brand=await BrandIdAsync(user,ct)??Guid.Empty;
  return await db.Database.SqlQuery<BrandBalanceRow>($"SELECT * FROM dbo.BrandBalances WHERE BrandProfileId={brand}").SingleOrDefaultAsync(ct)??new(){BrandProfileId=brand};
 }
 public Task<BrandBalancePolicy> PolicyAsync(CancellationToken ct)=>db.Database.SqlQuery<BrandBalancePolicy>($"SELECT * FROM dbo.BrandBalancePolicies WHERE Id=1").SingleAsync(ct);
 public Task<List<BrandWithdrawal>> WithdrawalsAsync(Guid? brand,CancellationToken ct)=>brand.HasValue
  ?db.Database.SqlQuery<BrandWithdrawal>($"SELECT TOP(100) * FROM dbo.BrandBalanceWithdrawals WHERE BrandProfileId={brand.Value} ORDER BY CreatedAtUtc DESC").ToListAsync(ct)
  :db.Database.SqlQuery<BrandWithdrawal>($"SELECT TOP(200) * FROM dbo.BrandBalanceWithdrawals ORDER BY CreatedAtUtc DESC").ToListAsync(ct);
 public Task<List<BrandBalanceEntry>> EntriesAsync(Guid brand,CancellationToken ct)=>db.Database.SqlQuery<BrandBalanceEntry>($"SELECT TOP(100) * FROM dbo.BrandBalanceLedgers WHERE BrandProfileId={brand} ORDER BY CreatedAtUtc DESC").ToListAsync(ct);

 // The account row is the serialization point for all spending and withdrawal reservations.
 public static async Task<BrandBalanceRow> LockAsync(TopstarDbContext db,Guid brand,CancellationToken ct)
 {
  await db.Database.ExecuteSqlInterpolatedAsync($"IF NOT EXISTS(SELECT 1 FROM dbo.BrandBalances WITH(UPDLOCK,HOLDLOCK) WHERE BrandProfileId={brand}) INSERT dbo.BrandBalances(BrandProfileId,Available,Reserved,UpdatedAtUtc) VALUES({brand},0,0,SYSUTCDATETIME())",ct);
  return await db.Database.SqlQuery<BrandBalanceRow>($"SELECT * FROM dbo.BrandBalances WITH(UPDLOCK,HOLDLOCK) WHERE BrandProfileId={brand}").SingleAsync(ct);
 }
 public static async Task WriteAsync(TopstarDbContext db,BrandBalanceRow account,decimal availableDelta,decimal reservedDelta,string type,Guid source,Guid operation,string note,Guid actor,CancellationToken ct)
 {
  var available=account.Available+availableDelta;var reserved=account.Reserved+reservedDelta;
  if(available<0||reserved<0)throw new InvalidOperationException("Số dư không đủ để thực hiện.");
  var id=Guid.NewGuid();var now=DateTime.UtcNow;
  await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalances SET Available={available},Reserved={reserved},UpdatedAtUtc={now} WHERE BrandProfileId={account.BrandProfileId}",ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandBalanceLedgers(Id,BrandProfileId,EntryType,Amount,AvailableAfter,ReservedAfter,SourceId,OperationKey,Note,ActorUserId,CreatedAtUtc) VALUES({id},{account.BrandProfileId},{type},{availableDelta},{available},{reserved},{source},{operation},{note},{actor},{now})",ct);
  account.Available=available;account.Reserved=reserved;
 }
 private void Notify(Guid brandUser,string title,string body,Guid source)=>db.Notifications.Add(new(){Id=Guid.NewGuid(),UserId=brandUser,Type="Payment",Title=title,Body=body,AudienceRole="BRAND",IsVisible=true,SourceEntityType="BrandBalance",SourceEntityId=source,TargetUrl="/Brand/Balance",CreatedAtUtc=DateTime.UtcNow});
 private void Audit(Guid actor,Guid source,string action,object value)=>db.AuditLogs.Add(new(){ActorUserId=actor,EntityName="BrandBalance",EntityId=source.ToString(),ActionName=action,PayloadJson=JsonSerializer.Serialize(value),CreatedAtUtc=DateTime.UtcNow});

 private async Task<BrandBalanceResult> CreditRefundCoreAsync(Guid admin,Guid fundingId,decimal amount,Guid operation,CancellationToken ct)
 {
  if(!BrandBalanceRules.ValidAmount(amount)||operation==Guid.Empty)return new(false,"Số tiền hoàn không hợp lệ.");
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  var f=await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={fundingId}").SingleOrDefaultAsync(ct);
  if(f is null||f.Status is not ("Paid" or "PartiallyReleased"))return new(false,"Khoản ký quỹ không ở trạng thái có thể đối soát hoàn.");
  var campaign=await db.Campaigns.SingleAsync(x=>x.Id==f.CampaignId,ct);
  var brand=await db.BrandProfiles.SingleAsync(x=>x.Id==campaign.BrandProfileId,ct);
  var account=await LockAsync(db,brand.Id,ct);
  if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceLedgers WHERE OperationKey={operation}").SingleAsync(ct)>0)return new(true,"Khoản hoàn này đã được ghi nhận.");
  var participations=db.Participations.Where(x=>x.CampaignId==campaign.Id).Select(x=>x.Id);
  if(await db.DisputeCases.AnyAsync(x=>participations.Contains(x.ParticipationId)&&x.IsActive&&x.Status!="Resolved"&&x.Status!="Cancelled",ct))return new(false,"Chiến dịch còn tranh chấp chưa xử lý; chưa thể chuyển tiền về số dư.");
  var allocations=await db.CampaignFundingAllocations.Where(x=>x.FundingId==fundingId).ToListAsync(ct);
  var allowed=BrandBalanceRules.Refundable(f.CampaignBudgetSnapshot,allocations.Sum(x=>x.Amount),allocations.Sum(x=>x.RefundedAmount),f.RefundedAmount,campaign.Status=="Cancelled");
  if(amount>allowed)return new(false,$"Số tiền vượt khoản có thể hoàn ({allowed:N0} đ). Cần chốt nghiệm thu hoặc hủy chiến dịch trước.");
  // Independent second approval for large credits; reuse immutable audit rather than accepting a client flag.
  var policy=await PolicyAsync(ct);
  if(amount>=policy.DualApprovalThreshold)
  {
   var key="BrandBalanceCredit:"+operation.ToString("N");
   var previous=await db.AuditLogs.Where(x=>x.EntityId==key&&x.ActionName=="ApproveRefundCredit").FirstOrDefaultAsync(ct);
   if(previous is null){db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="BrandBalance",EntityId=key,ActionName="ApproveRefundCredit",PayloadJson=JsonSerializer.Serialize(new{fundingId,amount}),CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã ghi nhận duyệt lần một. Cần quản trị viên khác duyệt cùng mã yêu cầu và số tiền.");}
   if(previous.ActorUserId==admin||previous.PayloadJson!=JsonSerializer.Serialize(new{fundingId,amount}))return new(false,"Cần người duyệt thứ hai khác người đầu và giữ nguyên khoản hoàn.");
  }
  await WriteAsync(db,account,amount,0,"RefundCredit",fundingId,operation,"Khoản đối soát hoàn từ ký quỹ chiến dịch.",admin,ct);
  f.RefundedAmount+=amount;f.RefundedAtUtc=DateTime.UtcNow;f.RefundReference="BAL-"+operation.ToString("N");
  f.Status=f.RefundedAmount==f.CampaignBudgetSnapshot?"Refunded":"PartiallyReleased";f.UpdatedAtUtc=DateTime.UtcNow;f.UpdatedByUserId=admin;
  db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=fundingId,EntryType="RefundToBalance",Amount=-amount,Note=f.RefundReference,ActorUserId=admin,CreatedAtUtc=DateTime.UtcNow});
  Audit(admin,fundingId,"RefundToBalance",new{amount,operation});
  Notify(brand.UserId,"Đã hoàn vào số dư Brand",$"Đã cộng {amount:N0} đ từ khoản ký quỹ được đối soát. Bạn có thể dùng lại hoặc yêu cầu nhận tiền về.",fundingId);
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã chuyển khoản hoàn vào số dư Brand.");
 }

 private async Task<BrandBalanceResult> PayFundingCoreAsync(Guid user,Guid fundingId,decimal amount,Guid operation,CancellationToken ct)
 {
  if(!BrandBalanceRules.ValidAmount(amount)||operation==Guid.Empty)return new(false,"Số tiền sử dụng không hợp lệ.");
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  var f=await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE Id={fundingId}").SingleOrDefaultAsync(ct);
  var brandId=await BrandIdAsync(user,ct);
  if(f is null||!brandId.HasValue||!await db.Campaigns.AnyAsync(x=>x.Id==f.CampaignId&&x.BrandProfileId==brandId.Value,ct))return new(false,"Không có quyền thanh toán yêu cầu này.");
  var account=await LockAsync(db,brandId.Value,ct);
  if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceLedgers WHERE OperationKey={operation}").SingleAsync(ct)>0)return new(true,"Thanh toán đã được ghi nhận.");
  if(f.Status!="Pending"||f.PaymentMethod=="OnePay"||f.OnlinePaymentStartedAtUtc.HasValue||f.PaymentProofSubmittedAtUtc.HasValue)return new(false,"Chỉ sử dụng số dư trước khi bắt đầu thanh toán bên ngoài hoặc gửi chứng từ. Hãy hủy yêu cầu chưa trả tiền để lập lại nếu cần.");
  var due=f.TotalPayableSnapshot-f.BalanceAppliedAmount;
  if(!BrandBalanceRules.CanSpend(amount,account.Available,due))return new(false,"Số tiền vượt số dư khả dụng hoặc số tiền còn phải thanh toán.");
  if(!await db.Campaigns.AnyAsync(x=>x.Id==f.CampaignId&&(x.Status=="Published"||x.Status=="InProgress"||(x.CheckoutAtCreation&&x.Status=="Draft")),ct))return new(false,"Chiến dịch không còn ở trạng thái có thể thanh toán.");
  await WriteAsync(db,account,-amount,0,"FundingPayment",fundingId,operation,"Sử dụng số dư thanh toán ký quỹ.",user,ct);
  f.BalanceAppliedAmount+=amount;f.UpdatedAtUtc=DateTime.UtcNow;
  if(f.BalanceAppliedAmount==f.TotalPayableSnapshot){f.Status="Paid";f.PaidAtUtc=DateTime.UtcNow;f.PaymentReference="BAL-"+operation.ToString("N");}
  db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=fundingId,EntryType="PaidBrandBalance",Amount=amount,Note=operation.ToString(),ActorUserId=user,CreatedAtUtc=DateTime.UtcNow});
  Audit(user,fundingId,"PayFundingBalance",new{amount,operation});
  Notify(user,"Đã sử dụng số dư Brand",$"Đã sử dụng {amount:N0} đ cho ký quỹ. Còn cần thanh toán {f.TotalPayableSnapshot-f.BalanceAppliedAmount:N0} đ.",fundingId);
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,f.Status=="Paid"?"Đã thanh toán đủ ký quỹ bằng số dư.":"Đã sử dụng số dư. Vui lòng thanh toán phần còn lại theo hướng dẫn.");
 }

 private async Task<BrandBalanceResult> RequestWithdrawalCoreAsync(Guid user,decimal amount,Guid operation,CancellationToken ct)
 {
  if(operation==Guid.Empty)return new(false,"Yêu cầu không hợp lệ.");
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  var brand=await db.BrandProfiles.SingleOrDefaultAsync(x=>x.UserId==user,ct);
  if(brand is null)return new(false,"Không tìm thấy Brand.");
  var account=await LockAsync(db,brand.Id,ct);
  if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceWithdrawals WHERE RequestKey={operation}").SingleAsync(ct)>0)return new(true,"Yêu cầu đã được tạo.");
  var policy=await PolicyAsync(ct);
  if(!policy.WithdrawalsEnabled)return new(false,"Chức năng nhận yêu cầu rút tiền đang tạm đóng.");
  if(!BrandBalanceRules.CanWithdraw(amount,account.Available,policy.MinimumWithdrawal,policy.WithdrawalFee))return new(false,"Số tiền không hợp lệ, chưa đạt mức tối thiểu hoặc vượt số dư khả dụng.");
  if(string.IsNullOrWhiteSpace(brand.RefundBankName)||string.IsNullOrWhiteSpace(brand.RefundAccountNumber)||string.IsNullOrWhiteSpace(brand.RefundAccountHolder))return new(false,"Vui lòng bổ sung tài khoản nhận hoàn tiền trong Hồ sơ Brand.");
  var id=Guid.NewGuid();var now=DateTime.UtcNow;var dual=amount>=policy.DualApprovalThreshold;
  await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandBalanceWithdrawals(Id,BrandProfileId,RequestKey,Amount,Fee,BankName,AccountNumber,AccountHolder,Status,DualApprovalRequired,CreatedAtUtc) VALUES({id},{brand.Id},{operation},{amount},{policy.WithdrawalFee},{brand.RefundBankName},{brand.RefundAccountNumber},{brand.RefundAccountHolder},N'Pending',{dual},{now})",ct);
  await WriteAsync(db,account,-amount,amount,"WithdrawalReserved",id,operation,"Giữ tiền cho yêu cầu nhận về ngân hàng.",user,ct);
  Audit(user,id,"RequestWithdrawal",new{amount,policy.WithdrawalFee});
  Notify(user,"Đã tạo yêu cầu nhận tiền",$"Đã giữ {amount:N0} đ. Số tiền dự kiến nhận sau phí: {amount-policy.WithdrawalFee:N0} đ.",id);
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã tạo yêu cầu. Số tiền được giữ riêng và không thể sử dụng đồng thời.");
 }

 private async Task<BrandBalanceResult> ProcessWithdrawalCoreAsync(Guid actor,Guid id,string action,string? reference,string? evidence,string? note,bool isAdmin,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  var w=await db.Database.SqlQuery<BrandWithdrawal>($"SELECT * FROM dbo.BrandBalanceWithdrawals WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
  if(w is null)return new(false,"Không tìm thấy yêu cầu.");
  var owner=await db.BrandProfiles.Where(x=>x.Id==w.BrandProfileId).Select(x=>x.UserId).SingleAsync(ct);
  if(!isAdmin&&(owner!=actor||action!="Cancel"))return new(false,"Không có quyền xử lý yêu cầu.");
  var account=await LockAsync(db,w.BrandProfileId,ct);
  var now=DateTime.UtcNow;
  if(action=="Cancel"||action=="Reject")
  {
   if(action=="Cancel"&&w.Status!="Pending"||action=="Reject"&&w.Status is not ("Pending" or "Approved"))return new(false,"Yêu cầu không còn có thể hủy/từ chối.");
   var status=action=="Cancel"?"Cancelled":"Rejected";
   await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalanceWithdrawals SET Status={status},Note={note},UpdatedAtUtc={now} WHERE Id={id}",ct);
   await WriteAsync(db,account,w.Amount,-w.Amount,"WithdrawalReturned",id,Guid.NewGuid(),"Trả tiền giữ do yêu cầu bị hủy/từ chối.",actor,ct);
  }
  else if(action=="Approve"&&isAdmin)
  {
   if(w.Status!="Pending")return new(false,"Yêu cầu không còn chờ duyệt.");
   if(w.DualApprovalRequired&&!w.FirstApprovedBy.HasValue)
    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalanceWithdrawals SET FirstApprovedBy={actor},UpdatedAtUtc={now} WHERE Id={id}",ct);
   else
   {
    if(w.DualApprovalRequired&&w.FirstApprovedBy==actor)return new(false,"Cần quản trị viên thứ hai khác người duyệt đầu.");
    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalanceWithdrawals SET Status=N'Approved',ApprovedBy={actor},UpdatedAtUtc={now} WHERE Id={id}",ct);
   }
  }
  else if(action=="Paid"&&isAdmin)
  {
   if(w.Status!="Approved")return new(false,"Chỉ xác nhận chuyển tiền sau khi yêu cầu được duyệt.");
   reference=(reference??"").Trim();
   if(reference.Length is <3 or >100||string.IsNullOrWhiteSpace(evidence)||!evidence.StartsWith("/uploads/campaign-funding-evidence/",StringComparison.Ordinal))return new(false,"Cần mã giao dịch và chứng từ chuyển tiền.");
   await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalanceWithdrawals SET Status=N'Paid',PaymentReference={reference},EvidenceUrl={evidence},UpdatedAtUtc={now} WHERE Id={id}",ct);
   await WriteAsync(db,account,0,-w.Amount,"WithdrawalPaid",id,Guid.NewGuid(),"Đã chuyển tiền về tài khoản ngân hàng.",actor,ct);
  }
  else return new(false,"Thao tác không hợp lệ.");
  Audit(actor,id,action,new{w.Amount,w.Fee,reference,note});
  Notify(owner,"Cập nhật yêu cầu nhận tiền",action switch{"Paid"=>$"Đã chuyển {w.NetAmount:N0} đ về tài khoản ngân hàng.","Reject"=>"Yêu cầu bị từ chối. Tiền đã trả về số dư khả dụng.","Cancel"=>"Đã hủy yêu cầu và trả lại tiền vào số dư.",_=>"Yêu cầu đang được xét duyệt."},id);
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã cập nhật yêu cầu nhận tiền.");
 }

 public async Task<BrandBalanceResult> SavePolicyAsync(Guid actor,bool enabled,decimal minimum,decimal fee,decimal dual,int days,CancellationToken ct)
 {
  if(!BrandBalanceRules.ValidAmount(minimum)||fee<0||fee!=decimal.Truncate(fee)||!BrandBalanceRules.ValidAmount(dual)||days is <1 or >30)return new(false,"Thông số chính sách không hợp lệ.");
  var now=DateTime.UtcNow;
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BrandBalancePolicies SET WithdrawalsEnabled={enabled},MinimumWithdrawal={minimum},WithdrawalFee={fee},DualApprovalThreshold={dual},ProcessingDays={days},UpdatedByUserId={actor},UpdatedAtUtc={now} WHERE Id=1",ct);
  Audit(actor,Guid.Empty,"UpdatePolicy",new{enabled,minimum,fee,dual,days});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã cập nhật chính sách. Các yêu cầu cũ giữ nguyên phí và điều kiện đã chốt.");
 }
}


