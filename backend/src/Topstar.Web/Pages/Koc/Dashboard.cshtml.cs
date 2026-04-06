using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService) : PageModel
{
    public KocDashboardData Data { get; private set; } = new("KOC", 0, 0, 0m, 0m, []);

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            Data = new KocDashboardData("KOC", 0, 0, 0m, 0m, []);
            return;
        }

        Data = await dashboardDataService.GetKocDashboardAsync(parsedUserId, HttpContext.RequestAborted);
    }
}

