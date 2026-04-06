using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService) : BrandGuardedPageModel(dashboardDataService)
{
    public BrandDashboardData Data { get; private set; } = new("Brand", true, 0, 0, 0, 0m, []);

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
            Data = new BrandDashboardData("Brand", true, 0, 0, 0, 0m, []);
            return RedirectToPage("/Public/Login");
        }

        Data = await DashboardDataService.GetBrandDashboardAsync(parsedUserId, HttpContext.RequestAborted);
        return Page();
    }
}
