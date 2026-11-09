using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc.RazorPages;using Topstar.Application.Reporting;
namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class ReportsModel(IReportingService service):PageModel{public AdminOperationalReport Report{get;private set;}=new(0,0,0,0,0,0,0,0,0,0,0);public async Task OnGetAsync(CancellationToken ct)=>Report=await service.GetAdminReportAsync(ct);}
