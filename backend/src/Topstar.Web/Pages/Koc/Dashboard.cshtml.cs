using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService) : KocGuardedPageModel(dashboardDataService)
{
    public KocDashboardData Data { get; private set; } = new("KOC", true, 0, 0, 0m, 0m, []);

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false);
        if (redirect is not null)
        {
            return redirect;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userId, out var parsedUserId);
        Data = await DashboardDataService.GetKocDashboardAsync(parsedUserId, HttpContext.RequestAborted);
        return Page();
    }
}
