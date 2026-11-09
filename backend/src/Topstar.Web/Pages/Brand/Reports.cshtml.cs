using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.Rendering;using Topstar.Application.Profiles;using Topstar.Application.Reporting;using Topstar.Application.SocialSync;
namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class ReportsModel(IProfileAccessService access,IReportingService reporting,ISocialSyncOrchestrator sync):BrandGuardedPageModel(access)
{
 [TempData]public string? SyncMessage{get;set;}[BindProperty(SupportsGet=true)]public Guid? CampaignId{get;set;}public BrandReport Report{get;private set;}=new(0,0,0,0,0,0,0,0,0,null,[],[]);public List<SelectListItem> CampaignOptions{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;await LoadAsync(ct);return Page();}
 public async Task<IActionResult> OnPostSyncCampaignAsync(CancellationToken ct){var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;if(!CampaignId.HasValue){SyncMessage="Vui lòng chọn chiến dịch.";return RedirectToPage();}var result=await sync.RequestBrandCampaignSyncAsync(UserId(),CampaignId.Value,ct);SyncMessage=result.Message;return RedirectToPage(new { campaignId=CampaignId });}
 private async Task LoadAsync(CancellationToken ct){Report=await reporting.GetBrandReportAsync(UserId(),ct,CampaignId);CampaignOptions=Report.Campaigns.Select(x=>new SelectListItem(x.Title,x.Id.ToString(),CampaignId==x.Id)).ToList();}private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
