using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Topstar.Application.Campaigns;
using Topstar.Application.Profiles;
using Topstar.Application.Common;
using Topstar.Web.Features.Common.Paging;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class JobsModel(IProfileAccessService profileAccessService, ICampaignService campaignService, ICampaignApplicationService applicationService) : KocGuardedPageModel(profileAccessService)
{
    public IReadOnlyList<CampaignListItem> Campaigns { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; } = 1;
    public IReadOnlyList<KocApplicationItem> Applications { get; private set; } = [];
    public ListWindow ApplicationWindow { get; private set; } = new();
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }
    [BindProperty(SupportsGet=true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet=true)] public string Logistics { get; set; }="all";
    [BindProperty(SupportsGet=true)] public decimal? MinimumBudget { get; set; }
    [BindProperty(SupportsGet=true)] public decimal? MaximumBudget { get; set; }
    [BindProperty(SupportsGet=true)] public string? DeliverableType { get; set; }
    [BindProperty(SupportsGet=true)] public string Sort { get; set; }="newest";
    [BindProperty(SupportsGet=true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet=true)] public int PageSize { get; set; } = 12;
    [BindProperty(SupportsGet=true)] public Guid? FocusApplication { get; set; }
    [BindProperty(SupportsGet=true)] public string Section { get; set; } = "opportunities";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false, requireApproval: false);
        if (redirect is not null) return redirect;
        Section = Section == "applications" || FocusApplication.HasValue ? "applications" : "opportunities";
        Keyword=Clean(Keyword,120);
        DeliverableType=Clean(DeliverableType,80);
        MinimumBudget = MinimumBudget is > 0 ? MinimumBudget : null;
        MaximumBudget = MaximumBudget is > 0 ? MaximumBudget : null;
        if (MinimumBudget.HasValue && MaximumBudget.HasValue && MinimumBudget > MaximumBudget)
            (MinimumBudget, MaximumBudget) = (MaximumBudget, MinimumBudget);
        if(Logistics is not("all" or "booking" or "shipment"))Logistics="all";
        if(Sort is not("newest" or "budget_desc" or "budget_asc" or "ending"))Sort="newest";
        PageNumber=Math.Max(1,PageNumber);if(PageSize is not(12 or 24 or 48))PageSize=12;
        var userId = UserId();
        if (Section == "opportunities")
        {
            var booking=Logistics=="booking"?true:(bool?)null;var shipment=Logistics=="shipment"?true:(bool?)null;
            var result = await campaignService.GetPublishedAsync(userId,new(Keyword,MinimumBudget,MaximumBudget,booking,shipment,DeliverableType,Sort,PageNumber,PageSize),cancellationToken);
            Campaigns=result.Items;TotalCount=result.TotalCount;TotalPages=result.TotalPages;PageNumber=result.Page;PageSize=result.PageSize;
        }
        else
        {
            ApplicationWindow = ListWindowRequest.Read(Request, "kocApplicationList");
            Applications = await applicationService.GetMineAsync(userId, cancellationToken, ApplicationWindow);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostWithdrawAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationService.WithdrawAsync(id, UserId(), cancellationToken);
        MessageSuccess = result.Success;
        Message = result.Message; return RedirectToListState(("section", "applications"), ("focusApplication", id.ToString()));
    }

    public async Task<IActionResult> OnPostFavoriteAsync(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false, requireApproval: false);
        if (redirect is not null) return redirect;
        var result = await campaignService.ToggleFavoriteAsync(id, UserId(), cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Koc/Jobs");
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string? Clean(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim()[..Math.Min(value.Trim().Length,max)];
}
