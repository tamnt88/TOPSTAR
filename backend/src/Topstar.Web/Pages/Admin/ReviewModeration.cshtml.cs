using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Reviews;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class ReviewModerationModel(ICampaignReviewService reviewService) : PageModel
{
    public IReadOnlyList<AdminReviewItem> Items { get; private set; } = [];
    public IReadOnlyList<AdminReviewAppealItem> Appeals { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await reviewService.GetAdminReviewsAsync(cancellationToken);
        Items = string.IsNullOrWhiteSpace(Status) ? items : items.Where(x => x.Status == Status).ToList();
        Appeals = await reviewService.GetAdminAppealsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostModerateAsync(Guid id, string targetStatus, string? reason, CancellationToken cancellationToken)
    {
        var result = await reviewService.ModerateAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, targetStatus, reason, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { status = Status });
    }

    public async Task<IActionResult> OnPostResolveAppealAsync(Guid id, string targetStatus, string decisionNote, CancellationToken cancellationToken)
    {
        var result = await reviewService.ResolveAppealAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, targetStatus, decisionNote, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { status = Status });
    }
}
