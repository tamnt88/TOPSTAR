using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class AcademyModel(IDashboardDataService dashboardDataService) : KocGuardedPageModel(dashboardDataService)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false);
        return redirect ?? Page();
    }
}
