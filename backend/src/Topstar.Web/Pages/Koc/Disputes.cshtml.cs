using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Disputes;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Koc;

public sealed class DisputesModel(IProfileAccessService access, IDisputeService service) : KocGuardedPageModel(access)
{
    public Topstar.Application.Common.ListWindow Window { get; private set; } = new();
    public DisputePortal Portal { get; private set; } = new([], []);

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        Portal = await service.GetPortalAsync(UserId(), false, cancellationToken, Window);
        return Page();
    }

    public async Task<IActionResult> OnGetDataAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? query = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return new JsonResult(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Hồ sơ chưa đủ điều kiện sử dụng tính năng." }) { StatusCode = StatusCodes.Status403Forbidden };

        length = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length);
        var window = new Topstar.Application.Common.ListWindow { Page = Math.Max(0, start) / length + 1, Size = length, Search = query, Status = status };
        var portal = await service.GetPortalAsync(UserId(), false, cancellationToken, window);
        var total = window.Total;
        var filtered = window.Filtered;
        var page = portal.Cases
            .Select(x => new
            {
                x.Id,
                x.CampaignTitle,
                x.BrandName,
                x.Category,
                x.Subject,
                x.Status,
                x.Priority,
                x.CreatedAtUtc,
                HistoryCount = x.History.Count,
                CanCancel = DisputeTransitions.CanCancel(x.Status)
            });

        return new JsonResult(new
        {
            draw = Math.Max(0, draw),
            recordsTotal = total,
            recordsFiltered = filtered,
            data = page
        });
    }

    public Task<IActionResult> OnPostOpenAsync(
        Guid participationId,
        string category,
        string subject,
        string description,
        CancellationToken cancellationToken) =>
        Run(() => service.OpenAsync(UserId(), false, participationId, category, subject, description, cancellationToken));

    public Task<IActionResult> OnPostCancelAsync(Guid id, string reason, CancellationToken cancellationToken) =>
        Run(() => service.CancelAsync(UserId(), id, reason, cancellationToken));

    private async Task<IActionResult> Run(Func<Task<DisputeResult>> action)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await action();
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
