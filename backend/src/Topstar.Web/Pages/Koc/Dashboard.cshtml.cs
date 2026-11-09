using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class DashboardModel(IDashboardDataService dashboardDataService, IProfileAccessService profileAccessService) : KocGuardedPageModel(profileAccessService)
{
    public KocDashboardData Data { get; private set; } = new("KOC", true, 0, 0, 0m, 0m, [], 0m, 0, 0, 0, 0, 0m, "Draft", [], []);
    public bool CanUseProtectedFeatures { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false, requireApproval: false);
        if (redirect is not null)
        {
            return redirect;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userId, out var parsedUserId);
        CanUseProtectedFeatures = (await ProfileAccessService.CheckKocAsync(parsedUserId, cancellationToken)).CanUseFeatures;
        Data = await dashboardDataService.GetKocDashboardAsync(parsedUserId, cancellationToken);
        ViewData["KocUnreadNotificationCount"] = Data.UnreadNotificationCount;
        return Page();
    }
}
