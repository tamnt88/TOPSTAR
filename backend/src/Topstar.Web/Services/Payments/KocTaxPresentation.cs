using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Services.Payments;
public sealed record KocTaxDisplay(decimal Gross,decimal? Tax,decimal? Net,bool Final,string Label){public decimal PlatformFee{get;init;}};
public static class KocTaxPresentation{
 public static async Task<KocTaxDisplay?> ReadAsync(TopstarDbContext db,Guid participationId,decimal? proposed,CancellationToken ct){
  var part=await db.Participations.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==participationId,ct);if(part is null)return null;
  var wallet=await db.WalletTransactions.AsNoTracking().SingleOrDefaultAsync(w=>w.ParticipationId==participationId,ct);
  if(wallet is not null){var gross=wallet.GrossAmountSnapshot??wallet.Amount;return wallet.TaxState=="Awaiting"?new KocTaxDisplay(gross,null,null,false,"Chờ xác minh thuế"){PlatformFee=wallet.PlatformFeeAmountSnapshot}:new KocTaxDisplay(gross,wallet.TaxState=="Calculated"?wallet.TaxAmountSnapshot:null,wallet.Amount,true,"Đã chốt khi cộng ví"){PlatformFee=wallet.PlatformFeeAmountSnapshot};}
  var amount=proposed??await (from a in db.CampaignFundingAllocations.AsNoTracking() join f in db.CampaignFundings on a.FundingId equals f.Id where a.ParticipationId==participationId&&f.Status!="Cancelled" orderby f.VersionNo descending select (decimal?)a.Amount).FirstOrDefaultAsync(ct);
  if(!amount.HasValue||amount<=0)return null;
  var rate=await db.Campaigns.Where(c=>c.Id==part.CampaignId).Select(c=>c.KocPlatformFeeRateSnapshot).SingleAsync(ct);
  var fee=decimal.Round(amount.Value*rate/100,0,MidpointRounding.AwayFromZero);
  var exists=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocIncomeTaxPolicies')").AnyAsync(v=>v>0,ct);if(!exists)return new KocTaxDisplay(amount.Value,null,null,false,"Thuế được chốt khi cộng ví"){PlatformFee=fee};
  var now=DateTime.UtcNow;var policy=await db.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies WHERE EffectiveAtUtc<={now}").OrderByDescending(p=>p.EffectiveAtUtc).ThenByDescending(p=>p.CreatedAtUtc).FirstOrDefaultAsync(ct);
  if(policy?.IsEnabled!=true)return new KocTaxDisplay(amount.Value,0,amount.Value-fee,false,"Dự kiến theo chính sách chưa bật khấu trừ"){PlatformFee=fee};
  var treatment=await db.Database.SqlQuery<KocTaxTreatment>($"SELECT * FROM dbo.KocTaxTreatments WHERE KocProfileId={part.KocProfileId}").SingleOrDefaultAsync(ct);
  var identity=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.KocTaxProfiles WHERE KocProfileId={part.KocProfileId}").AnyAsync(v=>v>0,ct);
  if(treatment is null||!identity)return new KocTaxDisplay(amount.Value,null,null,false,"Chờ xác minh thuế"){PlatformFee=fee};
  var tax=treatment.Treatment=="Withholding"?KocIncomeTax.Calculate(amount.Value,policy.Rate,policy.Threshold):0;return new KocTaxDisplay(amount.Value,tax,amount.Value-tax-fee,false,"Dự kiến — chốt khi cộng ví"){PlatformFee=fee};
 }
}
