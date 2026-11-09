using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Schedules;
namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class CalendarModel(IScheduleWorkflowService service):PageModel
{
 public ScheduleCalendarView Calendar{get;private set;}=new(new DateOnly(DateTime.Today.Year,DateTime.Today.Month,1),[]);[BindProperty(SupportsGet=true)]public DateOnly? Month{get;set;}
 public async Task OnGetAsync(CancellationToken ct){var d=Month??DateOnly.FromDateTime(DateTime.Today);Calendar=await service.GetCalendarAsync(null,"ADMIN",new DateOnly(d.Year,d.Month,1),ct);}
}
