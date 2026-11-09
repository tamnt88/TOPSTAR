using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class CampaignCheckoutModel(IProfileAccessService access,TopstarDbContext db,ICampaignFundingService funding,BrandBalanceService balances):BrandGuardedPageModel(access){
 [BindProperty(SupportsGet=true)]public Guid CampaignId{get;set;}
 [BindProperty]public string PaymentMethod{get;set;}="BankTransfer";
 [BindProperty]public bool UseBalance{get;set;}
 [BindProperty]public bool Agree{get;set;}
 [BindProperty]public decimal ExpectedTotal{get;set;}
 [BindProperty]public Guid Operation{get;set;}=Guid.NewGuid();
 public CampaignEntity Campaign{get;private set;}=null!;
 public CampaignFundingItem? Funding{get;private set;}
 public PaymentMethodConfigurationEntity Config{get;private set;}=new();
 public decimal Available{get;private set;}
 public decimal Fee{get;private set;}public decimal Gateway{get;private set;}public BrandTaxQuote? Tax{get;private set;}
 public bool HasPaymentSnapshot=>Funding?.Status is "Pending" or "Paid" or "PartiallyReleased" or "Released" or "Refunded";
 public decimal Total=>HasPaymentSnapshot?Funding!.TotalPayableSnapshot:Campaign.BudgetAmount+Fee+(Tax?.AddedVat??0)+Gateway;
 [TempData]public string? Message{get;set;}
 private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private async Task<bool> Load(CancellationToken ct){
  var campaign=await (from c in db.Campaigns.AsNoTracking() join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id==CampaignId&&b.UserId==Actor&&c.CheckoutAtCreation select c).SingleOrDefaultAsync(ct);
  if(campaign is null)return false;Campaign=campaign;
  Funding=await funding.GetForBrandAsync(Actor,CampaignId,ct);
  Config=await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct)??new();
  Available=await db.Database.SqlQuery<decimal>($"SELECT Available AS Value FROM dbo.BrandBalances WHERE BrandProfileId={Campaign.BrandProfileId}").SingleOrDefaultAsync(ct);
  var now=DateTime.UtcNow;var p=await db.CampaignFeePolicies.AsNoTracking().Where(p=>p.Code=="DEFAULT"&&p.IsActive&&p.EffectiveAtUtc<=now).OrderByDescending(p=>p.EffectiveAtUtc).ThenByDescending(p=>p.CreatedAtUtc).FirstOrDefaultAsync(ct);
  if(p is not null){Fee=BillingRules.CalculateFee(Campaign.BudgetAmount,p.FeeRate,p.FixedFee,p.MinimumFee,p.MaximumFee);Gateway=PaymentMethod is "VnPay" or "OnePay"&&p.GatewayFeeBearer=="Brand"?decimal.Round((Campaign.BudgetAmount+Fee)*p.GatewayFeeRate,0,MidpointRounding.AwayFromZero):0;}
  Tax=await BrandTaxPricing.QuoteAsync(db,"Funding",Campaign.BudgetAmount+Fee,Fee,ct);return true;
 }
 public async Task<IActionResult> OnGetQuoteAsync(string method,CancellationToken ct){PaymentMethod=method;var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return new JsonResult(new{error="Không có quyền truy cập."}){StatusCode=403};if(!await Load(ct))return NotFound();return new JsonResult(new{total=Total,fee=HasPaymentSnapshot?Funding!.PlatformFeeSnapshot:Fee,vat=HasPaymentSnapshot?Funding!.VatAmount:Tax?.Vat??0,gateway=HasPaymentSnapshot?Funding!.GatewayFeeSnapshot:Gateway});}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct,string? method=null){var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;if(!await Load(ct))return NotFound();PaymentMethod=HasPaymentSnapshot?Funding!.PaymentMethod:method is "BankTransfer" or "VnPay" or "OnePay" or "BrandBalance"?method:Config.BankTransferEnabled?"BankTransfer":Config.VnPayEnabled?"VnPay":Config.OnePayEnabled?"OnePay":"BrandBalance";await Load(ct);ExpectedTotal=Total;return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){
  var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;if(!await Load(ct))return NotFound();
  if(Campaign.Status!="Draft")return RedirectToPage("/Brand/CampaignWorkspace",new{id=CampaignId});
  if(!Agree)ModelState.AddModelError(nameof(Agree),"Vui lòng xác nhận thông tin và chi phí chiến dịch.");
  if(Operation==Guid.Empty)ModelState.AddModelError("","Vui lòng tải lại trang.");
  if(PaymentMethod is not ("BankTransfer" or "VnPay" or "OnePay" or "BrandBalance"))ModelState.AddModelError(nameof(PaymentMethod),"Chọn phương thức thanh toán hợp lệ.");
  if(ExpectedTotal!=Total){ModelState.AddModelError("","Chi phí đã thay đổi theo phương thức hoặc chính sách. Vui lòng kiểm tra tổng mới và xác nhận lại.");ExpectedTotal=Total;ModelState.Remove(nameof(ExpectedTotal));}
  if(PaymentMethod=="BrandBalance"&&Available<Total)ModelState.AddModelError(nameof(PaymentMethod),"Số dư chưa đủ. Chọn chuyển khoản hoặc thanh toán online và dùng số dư cho một phần.");
  if(UseBalance&&PaymentMethod=="OnePay")ModelState.AddModelError(nameof(PaymentMethod),"Phương thức online này chưa hỗ trợ kết hợp số dư. Vui lòng bỏ chọn dùng số dư.");
  if(!ModelState.IsValid)return Page();
  if(Funding?.Status is "Paid" or "PartiallyReleased" or "Released")return RedirectToPage("/Brand/CampaignWorkspace",new{id=CampaignId});
  if(Funding?.Status is null||Funding.Id is null||Funding.Status=="Cancelled"){
   var prep=await funding.PrepareFullFundingAsync(Actor,CampaignId,ct,Campaign.BudgetAmount);if(!prep.Success){ModelState.AddModelError("",prep.Message);return Page();}
  }
  Funding=await funding.GetForBrandAsync(Actor,CampaignId,ct);
  if(Funding?.Status=="Draft"){
   var result=await funding.SubmitAsync(Actor,CampaignId,PaymentMethod,ct,ExpectedTotal);if(!result.Success){ModelState.AddModelError("",result.Message);return Page();}
   Funding=await funding.GetForBrandAsync(Actor,CampaignId,ct);
  }
  if(Funding?.Id is not Guid id)return BadRequest();
  // Retries keep the originally selected method and signed payment totals.
  if(UseBalance||Funding.PaymentMethod=="BrandBalance"){
   var due=Funding.RemainingPayment;var spend=Math.Min(Available,due);
   if(Funding.PaymentMethod=="BrandBalance"&&Available<due){ModelState.AddModelError("","Số dư chưa đủ. Chọn phương thức khác bằng cách hủy yêu cầu chưa thanh toán.");return Page();}
   if(spend>0){var paid=await balances.PayFundingAsync(Actor,id,spend,Operation,ct);if(!paid.Success){ModelState.AddModelError("",paid.Message);return Page();}}
  }
  Funding=await funding.GetForBrandAsync(Actor,CampaignId,ct);
  if(Funding?.Status=="Paid")return RedirectToPage("/Brand/CampaignWorkspace",new{id=CampaignId});
  if(Funding?.PaymentMethod=="VnPay"){
   var pay=await HttpContext.RequestServices.GetRequiredService<Topstar.Web.Services.Payments.VnPayService>().CreateAsync(Actor,id,true,HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);
   if(pay.Success&&pay.RedirectUrl is not null)return Redirect(pay.RedirectUrl);ModelState.AddModelError("",pay.Message);return Page();
  }
  if(Funding?.PaymentMethod=="OnePay"){
   var pay=await HttpContext.RequestServices.GetRequiredService<Topstar.Web.Services.Payments.IOnePayService>().CreatePaymentUrlAsync(Actor,id,Config.OnePayDomesticEnabled?"DOMESTIC":Config.OnePayInternationalEnabled?"INTERNATIONAL":"QR",HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);
   if(pay.Success&&pay.RedirectUrl is not null)return Redirect(pay.RedirectUrl);ModelState.AddModelError("",pay.Message);return Page();
  }
  return RedirectToPage("/Brand/CampaignFunding",new{campaignId=CampaignId});
 }
}
