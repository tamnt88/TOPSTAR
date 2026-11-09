using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class MatchingModel(IMatchingService matchingService) : PageModel
{
    public IReadOnlyList<MatchingCampaignItem> Items { get; private set; } = [];
    [Microsoft.AspNetCore.Mvc.TempData] public string? Message { get; set; }
    public async Task OnGetAsync(CancellationToken cancellationToken) => Items = await matchingService.GetAdminCampaignsAsync(cancellationToken);
}
