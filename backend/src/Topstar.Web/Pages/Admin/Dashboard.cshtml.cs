using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService) : PageModel
{
    public AdminDashboardData Data { get; private set; } =
        new(0, 0, 0, 0m, 0, 0, 0, [], []);

    public async Task OnGetAsync()
    {
        Data = await dashboardDataService.GetAdminDashboardAsync(HttpContext.RequestAborted);
    }
}

