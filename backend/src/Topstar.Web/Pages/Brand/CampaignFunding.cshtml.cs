using System.Globalization;using System.Security.Claims;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using Topstar.Application.Billing;using Topstar.Application.Profiles;using Topstar.Infrastructure.Entities;using Topstar.Infrastructure.Persistence;using Topstar.Web.Services.Payments;using Topstar.Web.Services.Storage;
namespace Topstar.Web.Pages.Brand;
public sealed class CampaignFundingModel(IProfileAccessService access,ICampaignFundingService service,TopstarDbContext db,IOnePayService onePay,IFileStorageService storage):BrandGuardedPageModel(access)
{
 public bool CreationCheckout{get;private set;}
 public CampaignFeePolicyEntity? FeePolicy {get;private set;}
 public string? RefundBankName {get;private set;} public string? RefundAccountNumber {get;private set;} public string? RefundAccountHolder {get;private set;}
 public bool HasRefundAccount=>!string.IsNullOrWhiteSpace(RefundBankName)&&!string.IsNullOrWhiteSpace(RefundAccountNumber)&&!string.IsNullOrWhiteSpace(RefundAccountHolder);
 public Topstar.Infrastructure.Finance.BrandTaxQuote? TaxQuote{get;private set;}
 public decimal PreviewVatAdded=>TaxQuote?.AddedVat??0;
 public decimal PreviewPlatformFee => Selected is null || FeePolicy is null ? 0 : BillingRules.CalculateFee(Selected.CampaignBudget,FeePolicy.FeeRate,FeePolicy.FixedFee,FeePolicy.MinimumFee,FeePolicy.MaximumFee);
 public decimal PreviewGatewayFee => Selected is null || FeePolicy is null || FeePolicy.GatewayFeeBearer!="Brand" ? 0 : decimal.Round((Selected.CampaignBudget+PreviewPlatformFee)*FeePolicy.GatewayFeeRate,0,MidpointRounding.AwayFromZero);
 public Topstar.Application.Common.ListWindow Window{get;private set;}=new();[BindProperty(SupportsGet=true)]public Guid? CampaignId{get;set;}public IReadOnlyList<CampaignFundingItem> Items{get;private set;}=[];public CampaignFundingItem? Selected{get;private set;}public PaymentMethodConfigurationEntity PaymentConfig{get;private set;}=new();[TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;await Load(ct);return Page();}
 public async Task<IActionResult> OnPostSaveAsync(Guid campaignId,string allocationMode,CancellationToken ct){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;var custom=new Dictionary<Guid,decimal>();if(allocationMode==CampaignAllocationModes.Custom){foreach(var key in Request.Form.Keys.Where(x=>x.StartsWith("amount_",StringComparison.Ordinal))){if(Request.Form[key].Count!=1||!Guid.TryParse(key[7..],out var id)||!CampaignFundingRules.TryParseVnd(Request.Form[key].ToString(),out var amount)||!custom.TryAdd(id,amount)){Message="Thù lao phải là số tiền nguyên đồng hợp lệ và mỗi KOC chỉ có một khoản phân bổ.";Success=false;return RedirectToPage(new{campaignId});}}}var previouslyOffered=await (from a in db.CampaignFundingAllocations join p in db.Participations on a.ParticipationId equals p.Id where p.CampaignId==campaignId select p.Id).Distinct().ToArrayAsync(ct);CampaignFundingResult result;try{result=await service.SaveAllocationsAsync(UserId(),campaignId,allocationMode,custom,ct);}catch(Exception){Message="Không thể lưu bảng phân bổ vào lúc này. Vui lòng tải lại và kiểm tra trước khi thử lại.";Success=false;return RedirectToPage(new{campaignId});}if(result.Success&&await db.Campaigns.AnyAsync(c=>c.Id==campaignId&&c.CheckoutAtCreation,ct)){
 var inviteIds=await (from a in db.CampaignFundingAllocations join p in db.Participations on a.ParticipationId equals p.Id where p.CampaignId==campaignId&&p.Status=="Invited"&&!previouslyOffered.Contains(p.Id) select p.Id).Distinct().ToListAsync(ct);
 var mail=HttpContext.RequestServices.GetRequiredService<Topstar.Web.Services.Email.ICampaignParticipationEmailService>();
 foreach(var id in inviteIds)try{await mail.SendInvitationAsync(id,ct);}catch(Exception ex)when(ex is not OperationCanceledException){HttpContext.RequestServices.GetRequiredService<ILogger<CampaignFundingModel>>().LogError(ex,"Could not send allocated invitation {ParticipationId}",id);}
}Message=result.Message;Success=result.Success;return RedirectToPage(new{campaignId});}
 public async Task<IActionResult> OnPostSubmitAsync(Guid campaignId,string paymentMethod,decimal? expectedTotal,CancellationToken ct){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;if(!expectedTotal.HasValue||expectedTotal<=0){Message="Vui lòng kiểm tra tổng tiền ký quỹ trước khi xác nhận.";Success=false;return RedirectToPage(new{campaignId});}try{var result=await service.SubmitAsync(UserId(),campaignId,paymentMethod,ct,expectedTotal);Message=result.Message;Success=result.Success;}catch(Exception){Message="Không thể tạo yêu cầu thanh toán vào lúc này. Vui lòng tải lại và thử lại.";Success=false;}return RedirectToPage(new{campaignId});}
 public async Task<IActionResult> OnPostPrepareFullAsync(Guid campaignId,CancellationToken ct)
 {
     var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;
     decimal? amount=null;
     if(Request.Form.ContainsKey("prepayAmount"))
     {
         if(Request.Form["prepayAmount"].Count!=1 || !CampaignFundingRules.TryParseVnd(Request.Form["prepayAmount"].ToString(),out var parsed) || parsed<=0)
         { Message="Vui lòng nhập số tiền ký quỹ nguyên đồng, lớn hơn 0.";Success=false;return RedirectToPage(new{campaignId}); }
         amount=parsed;
     }
     var result=await service.PrepareFullFundingAsync(UserId(),campaignId,ct,amount);Message=result.Message;Success=result.Success;return RedirectToPage(new{campaignId});
 }
 public async Task<IActionResult> OnPostVnPayAsync(Guid fundingId,Guid campaignId,CancellationToken ct)
 {
     var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;
     var service=HttpContext.RequestServices.GetRequiredService<VnPayService>();
     var result=await service.CreateAsync(UserId(),fundingId,true,HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);
     if(result.Success&&result.RedirectUrl is not null)return Redirect(result.RedirectUrl);
     Message=result.Message;Success=false;return RedirectToPage(new{campaignId});
 }
 public async Task<IActionResult> OnPostOnePayAsync(Guid fundingId,Guid campaignId,string cardList,CancellationToken ct){var result=await onePay.CreatePaymentUrlAsync(UserId(),fundingId,cardList,HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);if(result.Success&&result.RedirectUrl is not null)return Redirect(result.RedirectUrl);Message=result.Message;Success=false;return RedirectToPage(new{campaignId});}
 public async Task<IActionResult> OnPostPaymentProofAsync(Guid fundingId,Guid campaignId,IFormFile? proofFile,string? proofNote,CancellationToken ct){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;proofNote=(proofNote??string.Empty).Trim();if(proofNote.Length<10||proofNote.Length>500){Message="Vui lòng nhập ghi chú chuyển khoản từ 10 đến 500 ký tự.";Success=false;return RedirectToPage(new{campaignId});}var url=await storage.SavePaymentProofAsync(proofFile,ct);if(url is null){Message="Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.";Success=false;return RedirectToPage(new{campaignId});}CampaignFundingResult result;try{result=await service.SubmitPaymentProofAsync(UserId(),fundingId,url,proofNote,ct);}catch{await storage.DeletePaymentProofAsync(url,ct);Message="Không thể gửi bằng chứng thanh toán vào lúc này.";Success=false;return RedirectToPage(new{campaignId});}if(!result.Success)await storage.DeletePaymentProofAsync(url,ct);Message=result.Message;Success=result.Success;return RedirectToPage(new{campaignId});}
 public async Task<IActionResult> OnPostCancelAsync(Guid fundingId,Guid campaignId,string cancellationReason,CancellationToken ct){var result=await service.CancelPendingAsync(UserId(),fundingId,cancellationReason,ct);Message=result.Message;Success=result.Success;return RedirectToPage(new{campaignId});}
 private async Task Load(CancellationToken ct){CreationCheckout=CampaignId.HasValue&&await db.Campaigns.AnyAsync(c=>c.Id==CampaignId&&c.CheckoutAtCreation,ct);Items=await service.GetForBrandAsync(UserId(),ct);Selected=CampaignId.HasValue?await service.GetForBrandAsync(UserId(),CampaignId.Value,ct):null;FeePolicy=await db.CampaignFeePolicies.AsNoTracking().Where(x=>x.Code=="DEFAULT"&&x.IsActive&&x.EffectiveAtUtc<=DateTime.UtcNow).OrderByDescending(x=>x.CreatedAtUtc).ThenByDescending(x=>x.EffectiveAtUtc).FirstOrDefaultAsync(ct);if(Selected is not null)TaxQuote=await Topstar.Infrastructure.Finance.BrandTaxPricing.QuoteAsync(db,"Funding",Selected.CampaignBudget+PreviewPlatformFee,PreviewPlatformFee,ct);PaymentConfig=await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct)??new();var refund=await db.BrandProfiles.AsNoTracking().Where(x=>x.UserId==UserId()).Select(x=>new{x.RefundBankName,x.RefundAccountNumber,x.RefundAccountHolder}).FirstOrDefaultAsync(ct);RefundBankName=refund?.RefundBankName;RefundAccountNumber=refund?.RefundAccountNumber;RefundAccountHolder=refund?.RefundAccountHolder;}
 private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task<IActionResult> OnPostBalanceAsync(Guid campaignId,Guid fundingId,string balanceAmount,Guid operation,CancellationToken ct)
 {
  var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
  if(!CampaignFundingRules.TryParseVnd(balanceAmount,out var amount)){Message="Số tiền sử dụng không hợp lệ.";Success=false;return RedirectToPage(new{campaignId});}
  var balances=HttpContext.RequestServices.GetRequiredService<Topstar.Infrastructure.Finance.BrandBalanceService>();
  var result=await balances.PayFundingAsync(UserId(),fundingId,amount,operation,ct);Message=result.Message;Success=result.Success;return RedirectToPage(new{campaignId});
 }
}
