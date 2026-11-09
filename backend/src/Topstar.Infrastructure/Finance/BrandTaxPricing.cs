using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Finance;
public sealed class BrandTaxPolicy{
 public Guid Id{get;set;}public string Scope{get;set;}="Package";public string VatMode{get;set;}="None";public decimal VatRate{get;set;}public string TaxBase{get;set;}="ServiceFee";public DateTime EffectiveAtUtc{get;set;}public DateTime CreatedAtUtc{get;set;}public Guid ActorUserId{get;set;}
}
public sealed record BrandTaxQuote(Guid? PolicyId,string Mode,decimal Rate,decimal TaxableBase,decimal Vat,decimal AddedVat,decimal Total);
public static class BrandTaxPricing{
 public static async Task<BrandTaxQuote> QuoteAsync(TopstarDbContext db,string scope,decimal baseAmount,decimal serviceFee,CancellationToken ct){
  var exists=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.BrandTaxPolicies')").AnyAsync(x=>x>0,ct);
  var p=exists ? await ReadEffectivePolicyAsync(db,scope,ct) : null;
  return Calculate(p,scope,baseAmount,serviceFee);
 }
 // Funding is the current Brand checkout. Before a Funding policy is configured,
 // reuse the effective legacy Brand policy, preserving its rate, mode and tax base.
 public static async Task<BrandTaxPolicy?> ReadEffectivePolicyAsync(TopstarDbContext db,string scope,CancellationToken ct){
  var now=DateTime.UtcNow;
  var policy=await db.Database.SqlQuery<BrandTaxPolicy>($"SELECT * FROM dbo.BrandTaxPolicies WHERE Scope={scope} AND EffectiveAtUtc<={now}").OrderByDescending(x=>x.EffectiveAtUtc).ThenByDescending(x=>x.CreatedAtUtc).FirstOrDefaultAsync(ct);
  if(policy is null && scope=="Funding")
   policy=await db.Database.SqlQuery<BrandTaxPolicy>($"SELECT * FROM dbo.BrandTaxPolicies WHERE Scope=N'Package' AND EffectiveAtUtc<={now}").OrderByDescending(x=>x.EffectiveAtUtc).ThenByDescending(x=>x.CreatedAtUtc).FirstOrDefaultAsync(ct);
  return policy;
 }
 public static BrandTaxQuote Calculate(BrandTaxPolicy? p,string scope,decimal baseAmount,decimal serviceFee){
  var taxable=scope=="Package"||p?.TaxBase=="Total"?baseAmount:serviceFee;
  var rate=p?.VatRate??0;var mode=p?.VatMode??"None";
  var vat=mode=="Included"?decimal.Round(taxable*rate/(100+rate),0,MidpointRounding.AwayFromZero):mode=="Excluded"?decimal.Round(taxable*rate/100,0,MidpointRounding.AwayFromZero):0;
  var extra=mode=="Excluded"?vat:0;return new(p?.Id,mode,rate,taxable,vat,extra,baseAmount+extra);
 }
}
