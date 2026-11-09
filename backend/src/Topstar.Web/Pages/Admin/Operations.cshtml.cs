using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Operations;
namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class OperationsModel(IOperationsAdminService service):PageModel
{
    public OperationsAdminDashboard Dashboard{get;private set;}=new([],[],[],[],[],[],[],[],[]);[TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
    public async Task OnGetAsync(CancellationToken ct)=>Dashboard=await service.GetDashboardAsync(ct);
    public async Task<IActionResult> OnPostShipmentAsync(Guid id,string decision,string note,CancellationToken ct)=>Done(await service.ResolveShipmentAsync(UserId(),id,decision,note,ct));
    public async Task<IActionResult> OnPostScheduleAsync(Guid id,string reason,CancellationToken ct)=>Done(await service.CancelScheduleAsync(UserId(),id,reason,ct));
    public async Task<IActionResult> OnPostScanAsync(CancellationToken ct)=>Done(await service.ScanSubmissionOverdueAsync(UserId(),ct));
    public async Task<IActionResult> OnPostAcknowledgeAsync(Guid id,string note,CancellationToken ct)=>Done(await service.AcknowledgeEscalationAsync(UserId(),id,note,ct));
    public async Task<IActionResult> OnPostScanDisputesAsync(CancellationToken ct)=>Done(await service.ScanDisputeSlaAsync(UserId(),ct));
    public async Task<IActionResult> OnPostScanBrandReviewsAsync(CancellationToken ct)=>Done(await service.ScanBrandReviewSlaAsync(UserId(),ct));
    public async Task<IActionResult> OnPostSlaPolicyAsync(Guid id,int highAfterHours,int criticalAfterHours,bool isActive,string note,CancellationToken ct)=>Done(await service.UpdateSlaPolicyAsync(UserId(),id,highAfterHours,criticalAfterHours,isActive,note,ct));
    private IActionResult Done(OperationsResult r){Message=r.Message;MessageSuccess=r.Success;return RedirectToPage();}private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
