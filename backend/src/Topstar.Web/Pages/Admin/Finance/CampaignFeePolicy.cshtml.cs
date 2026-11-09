using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Billing;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class CampaignFeePolicyModel(ICampaignFundingService service):PageModel
{
 public CampaignFeePolicyItem Policy{get;private set;}=null!;[TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 public async Task OnGetAsync(CancellationToken ct){if(Request.Path.Value?.Equals("/Admin/Finance/CampaignFeePolicy",StringComparison.OrdinalIgnoreCase)==true){Response.Redirect("/Admin/Finance/TaxFees?tab=Brand");return;} Policy=await service.GetPolicyAsync(ct);}
 public async Task<IActionResult> OnPostAsync(decimal feePercent,decimal fixedFee,decimal? minimumFee,decimal? maximumFee,decimal gatewayFeePercent,string gatewayFeeBearer,int disputeHoldDays,int paymentDueDays,decimal dualApprovalThreshold,string refundPolicy,DateTime effectiveAt,bool isActive,string reason,CancellationToken ct){if(!ModelState.IsValid||effectiveAt==default){Message="Vui lòng kiểm tra số tiền, tỷ lệ phí và thời điểm áp dụng.";Success=false;return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Brand"});}var effectiveUtc=DateTime.SpecifyKind(effectiveAt,DateTimeKind.Unspecified).AddHours(-7);var r=await service.UpdatePolicyAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),feePercent/100m,fixedFee,minimumFee,maximumFee,gatewayFeePercent/100m,gatewayFeeBearer,disputeHoldDays,paymentDueDays,dualApprovalThreshold,refundPolicy,effectiveUtc,isActive,reason,ct);Message=r.Message;Success=r.Success;return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Brand"});}
}
