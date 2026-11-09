using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Schedules;
namespace Topstar.Web.Pages.Brand;
public sealed class BookingsModel(IProfileAccessService access,IScheduleWorkflowService service):BrandGuardedPageModel(access)
{
 public Topstar.Application.Common.ListWindow Window { get; private set; } = new();

    public IReadOnlyList<ScheduleWorkItem> Items{get;private set;}=[];public ScheduleCalendarView Calendar{get;private set;}=new(new DateOnly(DateTime.Today.Year,DateTime.Today.Month,1),[]);[BindProperty(SupportsGet=true)]public DateOnly? Month{get;set;}[BindProperty(SupportsGet=true)]public Guid? CampaignId{get;set;}[TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
    public async Task<IActionResult> OnGetAsync(CancellationToken ct){ Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandBookingTable");var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;var month=NormalizeMonth(Month);Items=CampaignId.HasValue?await service.GetBrandItemsForCampaignAsync(UserId(),CampaignId.Value,ct, Window):await service.GetBrandItemsAsync(UserId(),ct, Window);Calendar=CampaignId.HasValue?await service.GetCalendarForCampaignAsync(UserId(),CampaignId.Value,month,ct):await service.GetCalendarAsync(UserId(),"BRAND",month,ct);return Page();}
    public Task<IActionResult> OnPostProposeAsync(Guid participationId,DateTime startsAtLocal,DateTime endsAtLocal,CancellationToken ct)=>Run(()=>service.ProposeAsync(UserId(),participationId,startsAtLocal,endsAtLocal,ct));
    public Task<IActionResult> OnPostConfirmAsync(Guid id,CancellationToken ct)=>Run(()=>service.ConfirmAsync(UserId(),true,id,ct));
    public Task<IActionResult> OnPostRescheduleAsync(Guid id,DateTime startsAtLocal,DateTime endsAtLocal,string note,CancellationToken ct)=>Run(()=>service.RescheduleAsync(UserId(),true,id,startsAtLocal,endsAtLocal,note,ct));
    public Task<IActionResult> OnPostCancelAsync(Guid id,string reason,CancellationToken ct)=>Run(()=>service.CancelAsync(UserId(),true,id,reason,ct));
    private async Task<IActionResult> Run(Func<Task<ScheduleResult>> action){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;try{return Done(await action());}catch(ScheduleConcurrencyException ex){return Done(new(false,ex.Message));}}
    private IActionResult Done(ScheduleResult r){Message=r.Message;MessageSuccess=r.Success;return RedirectToPage(new{campaignId=CampaignId});}private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private static DateOnly NormalizeMonth(DateOnly? value){var d=value??DateOnly.FromDateTime(DateTime.Today);return new(d.Year,d.Month,1);}
}
