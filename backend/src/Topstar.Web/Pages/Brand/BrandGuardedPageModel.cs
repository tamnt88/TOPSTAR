using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public abstract class BrandGuardedPageModel : PageModel
{
    protected BrandGuardedPageModel(IProfileAccessService profileAccessService)
    {
        ProfileAccessService = profileAccessService;
    }

    protected IProfileAccessService ProfileAccessService { get; }

    protected async Task<IActionResult?> RedirectIfProfileIncompleteAsync(bool allowProfilePages)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return RedirectToPage("/Public/Login");
        }

        var access = await ProfileAccessService.CheckBrandAsync(parsedUserId, HttpContext.RequestAborted);
        if (!allowProfilePages && !access.CanUseFeatures)
        {
            return RedirectToPage("/Brand/EditProfile", new { forceComplete = 1 });
        }

        return null;
    }
}
