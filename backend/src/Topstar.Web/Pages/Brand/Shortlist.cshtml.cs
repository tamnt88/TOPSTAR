using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Application.Profiles;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class ShortlistModel(
    IProfileAccessService profileAccessService,
    IMatchingService matchingService,
    ICampaignService campaignService,
    ICampaignParticipationEmailService participationEmail) : BrandGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public BrandShortlistDetail? Data { get; private set; }
    [TempData] public string? Message { get; set; }
    [TempData] public string? MessageType { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        if (Id == Guid.Empty)
            return Page();

        Data = await matchingService.GetBrandShortlistAsync(UserId(), Id, cancellationToken);
        return Data is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnGetDataAsync(
        int draw,
        int start,
        int length,
        string? keyword,
        string? status,
        int orderColumn,
        string? orderDirection,
        CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null)
            return new JsonResult(new { data = Array.Empty<object>(), message = "Hồ sơ Brand chưa đủ điều kiện sử dụng chức năng này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        var source = (await campaignService.GetBrandCampaignsAsync(UserId(), cancellationToken))
            .Where(x => x.Status is CampaignStatuses.Published or CampaignStatuses.InProgress or CampaignStatuses.Completed)
            .ToList();
        var totalCount = source.Count;
        var recruitingCount = source.Count(x => x.Status == CampaignStatuses.Published);
        var runningCount = source.Count(x => x.Status == CampaignStatuses.InProgress);
        var completedCount = source.Count(x => x.Status == CampaignStatuses.Completed);
        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            source = source.Where(x => x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        if (status is CampaignStatuses.Published or CampaignStatuses.InProgress or CampaignStatuses.Completed)
            source = source.Where(x => x.Status == status).ToList();

        var descending = string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase);
        source = (orderColumn, descending) switch
        {
            (1, false) => source.OrderBy(x => x.Title).ToList(),
            (1, true) => source.OrderByDescending(x => x.Title).ToList(),
            (2, false) => source.OrderBy(x => x.StartDate).ToList(),
            (2, true) => source.OrderByDescending(x => x.StartDate).ToList(),
            (3, false) => source.OrderBy(x => x.ApplicationCount).ToList(),
            (3, true) => source.OrderByDescending(x => x.ApplicationCount).ToList(),
            (4, false) => source.OrderBy(x => x.Status).ToList(),
            _ => source.OrderByDescending(x => x.Status).ThenByDescending(x => x.StartDate).ToList()
        };

        var filteredCount = source.Count;
        start = Math.Max(0, start);
        length = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length);
        var items = source.Skip(start).Take(length).Select((item, index) => new
        {
            item.Id,
            RowNumber = start + index + 1,
            item.Title,
            StartDate = item.StartDate?.ToString("dd/MM/yyyy"),
            EndDate = item.EndDate?.ToString("dd/MM/yyyy"),
            item.ApplicationCount,
            item.Status
        });

        return new JsonResult(new
        {
            draw = Math.Max(0, draw),
            recordsTotal = totalCount,
            recordsFiltered = filteredCount,
            data = items,
            summary = new
            {
                total = totalCount,
                recruiting = recruitingCount,
                running = runningCount,
                completed = completedCount
            }
        });
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await matchingService.ConfirmAsync(UserId(), applicationId, cancellationToken);
        Message = result.Message;
        MessageType = result.Success ? "success" : "error";
        if (result.Success && result.Id.HasValue)
            await participationEmail.SendInvitationAsync(result.Id.Value, cancellationToken);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostFinalizeAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await matchingService.FinalizeAsync(UserId(), Id, cancellationToken);
        Message = result.Message;
        MessageType = result.Success ? "success" : "error";
        if (result.Success)
            foreach (var participationId in result.RelatedIds)
                await participationEmail.SendInvitationAsync(participationId, cancellationToken);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid applicationId, string rejectionReason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await matchingService.RejectProposedAsync(UserId(), applicationId, rejectionReason, cancellationToken);
        Message = result.Message;
        MessageType = result.Success ? "success" : "error";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid applicationId, string removalReason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await matchingService.RemoveConfirmedAsync(UserId(), applicationId, removalReason, cancellationToken);
        Message = result.Message;
        MessageType = result.Success ? "success" : "error";
        return RedirectToPage(new { id = Id });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
