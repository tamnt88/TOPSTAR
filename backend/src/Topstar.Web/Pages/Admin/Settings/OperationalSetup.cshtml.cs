using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class OperationalSetupModel(IOperationalSetupService service):PageModel
{
    public OperationalPolicyItem Policy { get; private set; }=null!;
    public IReadOnlyList<OperationalCatalogItem> Catalogs { get; private set; }=[];
    public IReadOnlyList<OperationalCatalogHistoryItem> CatalogHistory { get; private set; }=[];
    [TempData]public string? Message{get;set;} [TempData]public bool Success{get;set;}
    public async Task OnGetAsync(CancellationToken ct){Policy=await service.GetPolicyAsync(ct);Catalogs=await service.GetCatalogAsync(null,false,ct);CatalogHistory=await service.GetCatalogHistoryAsync(ct);}
    public async Task<IActionResult> OnPostPolicyAsync(int reviewEditHours,int bookingLeadMinutes,int bookingMaxHours,int maxPayoutAccounts,int otpRequestLimit,int otpWindowMinutes,int otpExpiryMinutes,int otpMaxAttempts,string reason,CancellationToken ct)
    {var r=await service.UpdatePolicyAsync(UserId(),reviewEditHours,bookingLeadMinutes,bookingMaxHours,maxPayoutAccounts,otpRequestLimit,otpWindowMinutes,otpExpiryMinutes,otpMaxAttempts,reason,ct);Message=r.Message;Success=r.Success;if(!r.Success){await OnGetAsync(ct);return Page();}return RedirectToPage();}
    private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
