using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Collaborations;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class CollaborationsModel(
    IProfileAccessService profileAccessService,
    ICollaborationHistoryService service) : KocGuardedPageModel(profileAccessService)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        return redirect ?? Page();
    }

    public async Task<IActionResult> OnGetDataAsync(
        int draw=1,int start=0,int length=10,string? searchValue=null,string? status=null,
        CancellationToken cancellationToken=default)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null)
        {
            return new JsonResult(new { data = Array.Empty<object>(), message = "Hồ sơ chưa đủ điều kiện sử dụng tính năng." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var page = await service.GetKocPageAsync(userId, searchValue, status, start, length, cancellationToken);
        return new JsonResult(new
        {
            draw,
            recordsTotal = page.TotalCount,
            recordsFiltered = page.FilteredCount,
            data = page.Items.Select(item => new
            {
                participationId = item.ParticipationId,
                campaignTitle = item.CampaignTitle,
                brandName = item.BrandName,
                status = item.Status,
                confirmedAtUtc = item.ConfirmedAtUtc,
                currentStep = item.CurrentStep,
                waitingFor = item.WaitingFor,
                nextAction = item.NextAction,
                eventCount = item.Events.Count,
                events = item.Events.Select(history => new
                {
                    domain = history.Domain,
                    status = history.Status,
                    note = history.Note,
                    actorRole = history.ActorRole,
                    occurredAtUtc = history.OccurredAtUtc
                })
            })
        });
    }
}
