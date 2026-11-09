using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
using Topstar.Infrastructure.Entities;
namespace Topstar.Infrastructure.Finance;
public sealed class KocIncomeTaxPolicy{public Guid Id{get;set;}public bool IsEnabled{get;set;}public decimal Rate{get;set;}public decimal Threshold{get;set;}public DateTime EffectiveAtUtc{get;set;}public DateTime CreatedAtUtc{get;set;}public Guid ActorUserId{get;set;}}
public sealed class KocTaxTreatment{public Guid KocProfileId{get;set;}public string Treatment{get;set;}="";public string Reason{get;set;}="";public DateTime UpdatedAtUtc{get;set;}public Guid ActorUserId{get;set;}}
public static class KocIncomeTax{
 public static decimal Calculate(decimal gross,decimal rate,decimal threshold)=>gross>=threshold?decimal.Round(gross*rate/100,0,MidpointRounding.AwayFromZero):0;
 public static async Task ApplyAsync(TopstarDbContext db,CancellationToken ct){
  var earnings=db.ChangeTracker.Entries<WalletTransactionEntity>().Where(e=>(e.State==EntityState.Added&&(e.Entity.TransactionType=="Earning"||e.Entity.TransactionType=="CampaignEarning"))||(e.State==EntityState.Modified&&e.Entity.TaxState=="Awaiting"&&e.Entity.Status!="OnHold")).Select(e=>e.Entity).ToList();
  if(earnings.Count==0)return;
  var now=DateTime.UtcNow;var exists=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocIncomeTaxPolicies')").AnyAsync(x=>x>0,ct);
  KocIncomeTaxPolicy? policy=null;
  if(exists)policy=await db.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies WHERE EffectiveAtUtc<={now}").OrderByDescending(p=>p.EffectiveAtUtc).ThenByDescending(p=>p.CreatedAtUtc).FirstOrDefaultAsync(ct);
  foreach(var wallet in earnings){
   var appliedPolicy=policy;
   if(wallet.TaxState=="Awaiting"&&wallet.TaxPolicyIdSnapshot.HasValue)appliedPolicy=await db.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies WHERE Id={wallet.TaxPolicyIdSnapshot}").SingleAsync(ct);
   var gross=wallet.GrossAmountSnapshot??wallet.Amount;wallet.GrossAmountSnapshot=gross;
   if(wallet.TaxState=="Legacy"){
    var feeRate=await db.Campaigns.Where(c=>c.Id==wallet.CampaignId).Select(c=>c.KocPlatformFeeRateSnapshot).SingleAsync(ct);
    wallet.PlatformFeeRateSnapshot=feeRate;wallet.PlatformFeeAmountSnapshot=decimal.Round(gross*feeRate/100,0,MidpointRounding.AwayFromZero);
   }
   if(wallet.TaxState=="Legacy"&&appliedPolicy?.IsEnabled!=true){wallet.TaxState="NotApplied";wallet.Amount=gross-wallet.PlatformFeeAmountSnapshot;continue;}
   if(appliedPolicy is null)throw new InvalidOperationException("Chưa có chính sách thuế để đối soát thu nhập.");
   var treatment=await db.Database.SqlQuery<KocTaxTreatment>($"SELECT * FROM dbo.KocTaxTreatments WHERE KocProfileId={wallet.KocProfileId}").SingleOrDefaultAsync(ct);
   wallet.TaxPolicyIdSnapshot=appliedPolicy.Id;
   var hasIdentity=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.KocTaxProfiles WHERE KocProfileId={wallet.KocProfileId}").AnyAsync(x=>x>0,ct);
   if(treatment is null||!hasIdentity){wallet.TaxState="Awaiting";wallet.Status="OnHold";wallet.AvailableAtUtc=null;wallet.HoldReason="Chờ xác minh hồ sơ thuế KOC.";
    foreach(var history in db.ChangeTracker.Entries<WalletTransactionStatusHistoryEntity>().Where(e=>e.State==EntityState.Added&&e.Entity.WalletTransactionId==wallet.Id))history.Entity.ToStatus="OnHold";
    continue;}
   wallet.TaxPolicyIdSnapshot=appliedPolicy.Id;wallet.TaxTreatmentSnapshot=treatment.Treatment;wallet.TaxRateSnapshot=treatment.Treatment=="Withholding"?appliedPolicy.Rate:0;
   wallet.TaxAmountSnapshot=appliedPolicy.IsEnabled&&treatment.Treatment=="Withholding"?Calculate(gross,appliedPolicy.Rate,appliedPolicy.Threshold):0;
   wallet.Amount=gross-wallet.TaxAmountSnapshot-wallet.PlatformFeeAmountSnapshot;
   if(wallet.Amount<0)throw new InvalidOperationException("Tổng phí nền tảng và thuế vượt thù lao KOC. Vui lòng kiểm tra cấu hình.");wallet.TaxState="Calculated";
   if(wallet.HoldReason=="Chờ xác minh hồ sơ thuế KOC.")wallet.HoldReason=null;
   db.AuditLogs.Add(new(){ActorUserId=wallet.CreatedByUserId,EntityName="KocIncomeTax",EntityId=wallet.Id.ToString(),ActionName="CalculateAtWalletCredit",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{gross,wallet.TaxAmountSnapshot,Net=wallet.Amount,appliedPolicy.Id,treatment.Treatment}),CreatedAtUtc=now});
  }
 }
}
