using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Deliverables;
using Topstar.Application.Profiles;
using Topstar.Application.Schedules;
namespace Topstar.Web.Pages.Koc;
public sealed class BookingsModel(IProfileAccessService access,IScheduleWorkflowService service,IDeliverableWorkflowService deliverableService):KocGuardedPageModel(access)
{
 public Topstar.Application.Common.ListWindow Window { get; private set; } = new();

    public int OpenDeadlineCount{get;private set;}
    public IReadOnlyList<ScheduleWorkItem> Items{get;private set;}=[];public IReadOnlyList<KocDeliverableTask> Tasks{get;private set;}=[];public ScheduleCalendarView Calendar{get;private set;}=new(new DateOnly(DateTime.Today.Year,DateTime.Today.Month,1),[]);[BindProperty(SupportsGet=true)]public DateOnly? Month{get;set;}[TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
    public async Task<IActionResult> OnGetAsync(CancellationToken ct){ Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"kocBookingList");var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;var month=NormalizeMonth(Month);Items=await service.GetKocItemsAsync(UserId(),ct, Window);Tasks=await deliverableService.GetKocTasksAsync(UserId(),ct);OpenDeadlineCount=(await deliverableService.GetKocTaskPageAsync(UserId(),new(null,null,"seven_days","due",1,10),ct)).TotalCount;Calendar=await service.GetCalendarAsync(UserId(),"KOC",month,ct);return Page();}
    public Task<IActionResult> OnPostConfirmAsync(Guid id,CancellationToken ct)=>Run(()=>service.ConfirmAsync(UserId(),false,id,ct));
    public Task<IActionResult> OnPostRescheduleAsync(Guid id,DateTime startsAtLocal,DateTime endsAtLocal,string note,CancellationToken ct)=>Run(()=>service.RescheduleAsync(UserId(),false,id,startsAtLocal,endsAtLocal,note,ct));
    public Task<IActionResult> OnPostCancelAsync(Guid id,string reason,CancellationToken ct)=>Run(()=>service.CancelAsync(UserId(),false,id,reason,ct));
    private async Task<IActionResult> Run(Func<Task<ScheduleResult>> action){var r=await RedirectIfProfileIncompleteAsync(false);if(r is not null)return r;try{return Done(await action());}catch(ScheduleConcurrencyException ex){return Done(new(false,ex.Message));}}
    private IActionResult Done(ScheduleResult r){Message=r.Message;MessageSuccess=r.Success;return RedirectToListState();}private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private static DateOnly NormalizeMonth(DateOnly? value){var d=value??DateOnly.FromDateTime(DateTime.Today);return new(d.Year,d.Month,1);}
}
