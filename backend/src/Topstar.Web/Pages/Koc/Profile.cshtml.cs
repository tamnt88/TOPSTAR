using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class ProfileModel(IDashboardDataService dashboardDataService) : KocGuardedPageModel(dashboardDataService)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: true);
        return redirect ?? Page();
    }
}
