using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public abstract class BrandGuardedPageModel : PageModel
{
    protected BrandGuardedPageModel(IDashboardDataService dashboardDataService)
    {
        DashboardDataService = dashboardDataService;
    }

    protected IDashboardDataService DashboardDataService { get; }

    protected async Task<IActionResult?> RedirectIfProfileIncompleteAsync(bool allowProfilePages)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return RedirectToPage("/Public/Login");
        }

        var brandData = await DashboardDataService.GetBrandDashboardAsync(parsedUserId, HttpContext.RequestAborted);
        if (!allowProfilePages && brandData.NeedsProfileCompletion)
        {
            return RedirectToPage("/Brand/EditProfile", new { forceComplete = 1 });
        }

        return null;
    }
}
