using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class CampaignsModel(IDashboardDataService dashboardDataService) : BrandGuardedPageModel(dashboardDataService)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        return redirect ?? Page();
    }
}
