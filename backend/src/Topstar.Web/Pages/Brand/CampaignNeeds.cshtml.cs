using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Persistence;
using Topstar.Infrastructure.Finance;
namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class CampaignNeedsModel(IProfileAccessService access,TopstarDbContext db,IPackagePolicyAdminService packages):BrandGuardedPageModel(access){
 public CampaignCreationPolicy? Policy{get;private set;}
 public IReadOnlyList<PackagePolicyItem> Templates{get;private set;}=[];
 [BindProperty(SupportsGet=true)]public Guid? PackageId{get;set;}
 [BindProperty(SupportsGet=true)]public decimal Budget{get;set;}
 [BindProperty(SupportsGet=true)]public int KocCount{get;set;}=1;
 [BindProperty(SupportsGet=true)]public bool SelfSelect{get;set;}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;await Load(ct);if(PackageId.HasValue){var p=Templates.FirstOrDefault(p=>p.Id==PackageId);if(p is not null){Budget=p.ReferenceBudget??0;KocCount=p.SelectionQuota??1;SelfSelect=p.BrandSelectionEnabled;}}return Page();}
 public async Task<IActionResult> OnGetQuoteAsync(decimal budget,int count,bool selfSelect,CancellationToken ct){
  var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return new JsonResult(new{error="Không có quyền truy cập."}){StatusCode=403};
  var rule=await CampaignCreationRules.ReadAsync(db,ct);if(rule is null)return BadRequest();var error=CampaignCreationRules.Validate(rule,budget,count,selfSelect);if(error is not null)return new JsonResult(new{error});
  var now=DateTime.UtcNow;var feePolicy=await db.CampaignFeePolicies.AsNoTracking().Where(p=>p.Code=="DEFAULT"&&p.IsActive&&p.EffectiveAtUtc<=now).OrderByDescending(p=>p.EffectiveAtUtc).ThenByDescending(p=>p.CreatedAtUtc).FirstOrDefaultAsync(ct);
  if(feePolicy is null)return new JsonResult(new{error="Chưa có chính sách phí đang hiệu lực."});var fee=BillingRules.CalculateFee(budget,feePolicy.FeeRate,feePolicy.FixedFee,feePolicy.MinimumFee,feePolicy.MaximumFee);var tax=await BrandTaxPricing.QuoteAsync(db,"Funding",budget+fee,fee,ct);return new JsonResult(new{budget,fee,vat=tax.Vat,vatRate=tax.Rate,vatMode=tax.Mode,taxBase=tax.TaxableBase,total=tax.Total,maxKoc=CampaignCreationRules.MaximumKoc(rule,budget)});
 }
 private async Task Load(CancellationToken ct){Policy=await CampaignCreationRules.ReadAsync(db,ct);Templates=await packages.GetPublicAsync(ct);}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;await Load(ct);if(Policy is null){ModelState.AddModelError("", "Cần triển khai SQL 169 trước.");return Page();}var error=CampaignCreationRules.Validate(Policy,Budget,KocCount,SelfSelect);if(error is not null)ModelState.AddModelError("",error);if(!ModelState.IsValid)return Page();return RedirectToPage("/Brand/CampaignEdit",new{budget=Budget,kocCount=KocCount,selfSelect=SelfSelect});}
}
