using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Deliverables;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

public sealed class DeliverablesModel(IProfileAccessService profileAccessService, IDeliverableWorkflowService workflowService) : BrandGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    public IReadOnlyList<BrandSubmissionItem> Items { get; private set; } = [];
    public BrandCampaignExecutionSummary? Summary { get; private set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        Items = await workflowService.GetBrandSubmissionsAsync(UserId(), Id, HttpContext.RequestAborted);
        if (Id.HasValue) Summary = await workflowService.GetBrandCampaignSummaryAsync(UserId(), Id.Value, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await workflowService.CompleteCampaignAsync(UserId(), id, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReviewAsync(Guid id, Guid? campaignId, string decision, string? comment, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await workflowService.ReviewAsync(UserId(), id, decision, comment, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { id = campaignId });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
