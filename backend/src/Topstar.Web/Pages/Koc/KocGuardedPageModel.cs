using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public abstract class KocGuardedPageModel : PageModel
{
    protected KocGuardedPageModel(IDashboardDataService dashboardDataService)
    {
        DashboardDataService = dashboardDataService;
    }

    protected IDashboardDataService DashboardDataService { get; }

    protected async Task<IActionResult?> RedirectIfProfileIncompleteAsync(bool allowProfilePage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return RedirectToPage("/Public/Login");
        }

        var kocData = await DashboardDataService.GetKocDashboardAsync(parsedUserId, HttpContext.RequestAborted);
        if (!allowProfilePage && kocData.NeedsProfileCompletion)
        {
            return RedirectToPage("/Koc/Profile", new { forceComplete = 1 });
        }

        return null;
    }
}
