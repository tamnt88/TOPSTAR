using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class ProfilesModel(IProfileReviewService profileReviewService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    public IReadOnlyList<ProfileReviewListItem> Items { get; private set; } = [];
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Items = await profileReviewService.GetQueueAsync(Type, cancellationToken);
}
