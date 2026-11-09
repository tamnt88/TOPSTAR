using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Reviews;

namespace Topstar.Web.Pages.Brand;

public sealed class ReviewsModel(IProfileAccessService profileAccessService, ICampaignReviewService reviewService) : BrandGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public Guid? CampaignId { get; set; }
    public IReadOnlyList<BrandReviewTask> Items { get; private set; } = [];
    public IReadOnlyList<ReceivedReviewItem> ReceivedItems { get; private set; } = [];
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        Items = await reviewService.GetBrandTasksAsync(UserId(), CampaignId, HttpContext.RequestAborted);
        ReceivedItems = await reviewService.GetReceivedReviewsAsync(UserId(), CampaignId, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(Guid id, string body, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await reviewService.ReplyAsync(UserId(), id, body, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { campaignId = CampaignId });
    }

    public async Task<IActionResult> OnPostAppealAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await reviewService.AppealAsync(UserId(), id, reason, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { campaignId = CampaignId });
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, byte rating, string? comment, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await reviewService.ReviewKocAsync(UserId(), id, rating, comment, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { campaignId = CampaignId });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
