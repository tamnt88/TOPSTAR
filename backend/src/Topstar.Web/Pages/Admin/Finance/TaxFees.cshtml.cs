using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class TaxFeesModel(TopstarDbContext db,ICampaignFundingService funding,IWalletService wallet):PageModel
{
 [BindProperty(SupportsGet=true)] public string? Tab{get;set;}="Brand";
 [BindProperty] public decimal KocFeeRate{get;set;}
 [BindProperty] public string? VatMode{get;set;}="None";
 [BindProperty] public decimal VatRate{get;set;}
 [BindProperty] public string? TaxBase{get;set;}="ServiceFee";
 [BindProperty] public bool Enabled{get;set;}
 [BindProperty] public decimal Rate{get;set;}
 [BindProperty] public decimal Threshold{get;set;}
 [BindProperty] public DateTime EffectiveAt{get;set;}
 public bool PayoutReady{get;private set;}
 [TempData] public string? Message{get;set;}
 [TempData] public bool Success{get;set;}=true;
 public BrandTaxPolicyModel Vat{get;private set;}=null!;
 public CampaignFeePolicyModel BrandFee{get;private set;}=null!;
 public KocIncomeTaxModel KocTax{get;private set;}=null!;
 public PayoutPolicyModel Payout{get;private set;}=null!;
 public bool FeeReady{get;private set;}
 public async Task OnGetAsync(CancellationToken ct)=>await Load(ct);
 private async Task Load(CancellationToken ct){
  Tab=string.Equals(Tab,"Koc",StringComparison.OrdinalIgnoreCase)?"Koc":"Brand";
  if(Tab=="Brand"){
   Vat=new(db){PageContext=PageContext,Scope="Funding"};await Vat.OnGetAsync(ct);
   BrandFee=new(funding){PageContext=PageContext};await BrandFee.OnGetAsync(ct);
  }else{
   KocTax=new(db){PageContext=PageContext};await KocTax.OnGetAsync(ct);
   PayoutReady=await db.PayoutPolicies.AsNoTracking().AnyAsync(p=>p.Code=="DEFAULT",ct);
   if(PayoutReady){Payout=new(wallet){PageContext=PageContext};await Payout.OnGetAsync(ct);}
   var p=await CampaignCreationRules.ReadAsync(db,ct);FeeReady=p is not null;
   if(Request.Method=="GET")KocFeeRate=p?.KocPlatformFeeRate??0;
  }
 }
 public async Task<IActionResult> OnPostVatAsync(CancellationToken ct){
  Tab="Brand";
  var editor=new BrandTaxPolicyModel(db){PageContext=PageContext,Scope="Funding",VatMode=VatMode??"",VatRate=VatRate,TaxBase=TaxBase??"",EffectiveAt=EffectiveAt};
  var result=await editor.OnPostAsync(ct);
  if(result is not PageResult){Message=editor.Message;Success=true;return result;}
  await Load(ct);Vat=editor;return Page();
 }
 public async Task<IActionResult> OnPostKocTaxAsync(CancellationToken ct){
  Tab="Koc";
  var editor=new KocIncomeTaxModel(db){PageContext=PageContext,Enabled=Enabled,Rate=Rate,Threshold=Threshold,EffectiveAt=EffectiveAt};
  var result=await editor.OnPostAsync(ct);
  if(result is not PageResult){Message=editor.Message;Success=editor.Success;return result;}
  await Load(ct);KocTax=editor;return Page();
 }
 public async Task<IActionResult> OnPostKocFeeAsync(CancellationToken ct){
  Tab="Koc";
  if(KocFeeRate<0||KocFeeRate>99)ModelState.AddModelError(nameof(KocFeeRate),"Phí nền tảng phải từ 0 đến 99%.");
  var p=await CampaignCreationRules.ReadAsync(db,ct);
  if(p is null)ModelState.AddModelError("","Cần triển khai SQL 169 trước.");
  if(!ModelState.IsValid){await Load(ct);return Page();}
  var actor=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var now=DateTime.UtcNow;
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CampaignCreationPolicies SET KocPlatformFeeRate={KocFeeRate},UpdatedAtUtc={now},ActorUserId={actor} WHERE Id=1",ct);
  db.AuditLogs.Add(new(){ActorUserId=actor,EntityName="CampaignCreationPolicy",EntityId="1",ActionName="UpdateKocFee",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{Before=p!.KocPlatformFeeRate,After=KocFeeRate}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu phí nền tảng KOC.";Success=true;
  return RedirectToPage(new{tab="Koc"});
 }
}
