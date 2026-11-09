using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public abstract class KocGuardedPageModel : PageModel
{
    protected KocGuardedPageModel(IProfileAccessService profileAccessService)
    {
        ProfileAccessService = profileAccessService;
    }

    protected IProfileAccessService ProfileAccessService { get; }

    protected async Task<IActionResult?> RedirectIfProfileIncompleteAsync(bool allowProfilePage, bool requireApproval = true)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return RedirectToPage("/Public/Login");
        }

        var access = await ProfileAccessService.CheckKocAsync(parsedUserId, HttpContext.RequestAborted);
        var canContinue = requireApproval ? access.CanUseFeatures : access.CanBrowseFeatures;
        if (!allowProfilePage && !canContinue)
        {
            return RedirectToPage("/Koc/Profile", new { forceComplete = 1 });
        }

        return null;
    }

    protected IActionResult RedirectToListState(params (string Key, string? Value)[] values)
    {
        var query = Request.Query
            .Where(x => x.Key.StartsWith("list.", StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Value, (pair, value) => new KeyValuePair<string, string?>(pair.Key, value))
            .ToList();
        foreach (var (key, value) in values)
        {
            query.RemoveAll(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(value)) query.Add(new(key, value));
        }
        return Redirect(Request.PathBase + Request.Path + QueryString.Create(query));
    }
}
