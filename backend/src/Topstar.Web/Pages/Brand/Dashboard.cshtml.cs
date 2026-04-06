using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService) : PageModel
{
    public BrandDashboardData Data { get; private set; } = new("Brand", 0, 0, 0, 0m, []);

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            Data = new BrandDashboardData("Brand", 0, 0, 0, 0m, []);
            return;
        }

        Data = await dashboardDataService.GetBrandDashboardAsync(parsedUserId, HttpContext.RequestAborted);
    }
}

