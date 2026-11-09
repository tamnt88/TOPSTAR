using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Notifications;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Koc;

public sealed class NotificationsModel(
    IProfileAccessService access,
    INotificationInboxService service) : KocGuardedPageModel(access)
{
    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    public NotificationInbox Inbox { get; private set; } = new(0, []);
    public int ActiveCount { get; private set; }
    public int ArchivedCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(true);
        if (redirect is not null) return redirect;

        Filter = NormalizeFilter(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetDataAsync(int draw, int start, int length, string? query, string? filter, string? category, CancellationToken ct)
    {
        if (await RedirectIfProfileIncompleteAsync(true) is not null) return StatusCode(403);
        var result = await service.GetPageAsync(UserId(), "KOC", filter, query, start, length, ct, category);
        return new JsonResult(new { draw = Math.Max(0, draw), recordsTotal = result.TotalCount, recordsFiltered = result.FilteredCount, data = result.Items, summary = new { result.UnreadCount, result.ActiveCount, result.ArchivedCount } });
    }

    public async Task<JsonResult> OnPostReadAjaxAsync(Guid id, CancellationToken ct) =>
        Result(await service.MarkReadAsync(UserId(), "KOC", id, ct));

    public async Task<JsonResult> OnPostAllAjaxAsync(CancellationToken ct) =>
        Result(await service.MarkAllReadAsync(UserId(), "KOC", ct));

    public async Task<JsonResult> OnPostArchiveAjaxAsync(Guid id, CancellationToken ct) =>
        Result(await service.ArchiveAsync(UserId(), "KOC", id, ct));

    public async Task<JsonResult> OnPostRestoreAjaxAsync(Guid id, CancellationToken ct) =>
        Result(await service.RestoreAsync(UserId(), "KOC", id, ct));

    public async Task<JsonResult> OnPostOpenAjaxAsync(Guid id, CancellationToken ct)
    {
        var result = await service.OpenAsync(UserId(), "KOC", id, ct);
        return new JsonResult(new { success = result.Success, message = result.Message, targetUrl = result.TargetUrl });
    }

    private static JsonResult Result(NotificationResult result) =>
        new(new { success = result.Success, message = result.Message });

    private static string NormalizeFilter(string? filter) =>
        filter?.Trim().ToLowerInvariant() is "unread" or "archived" ? filter.Trim().ToLowerInvariant() : "all";

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
