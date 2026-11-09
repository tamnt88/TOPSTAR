using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService, IProfileAccessService profileAccessService) : BrandGuardedPageModel(profileAccessService)
{
    public BrandDashboardData Data { get; private set; } = new("Brand", true, 0, 0, 0, 0m, [], []);

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        if (redirect is not null)
        {
            return redirect;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            Data = new BrandDashboardData("Brand", true, 0, 0, 0, 0m, [], []);
            return RedirectToPage("/Public/Login");
        }

        Data = await dashboardDataService.GetBrandDashboardAsync(parsedUserId, HttpContext.RequestAborted);
        return Page();
    }
}
