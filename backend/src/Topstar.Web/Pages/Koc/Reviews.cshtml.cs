using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Reviews;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Koc;

public sealed class ReviewsModel(
    IProfileAccessService profileAccessService,
    ICampaignReviewService reviewService,
    ILogger<ReviewsModel> logger,IOperationalSetupService setup) : KocGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public byte? Rating { get; set; }
    [BindProperty(SupportsGet = true)] public string? State { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
    [BindProperty(SupportsGet = true)] public string? SentSearch { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? SentFromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? SentToDate { get; set; }
    [BindProperty(SupportsGet = true)] public byte? SentRating { get; set; }
    [BindProperty(SupportsGet = true)] public string? SentState { get; set; }
    [BindProperty(SupportsGet = true)] public int SentPageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int SentPageSize { get; set; } = 10;
    public IReadOnlyList<KocReviewTask> Items { get; private set; } = [];
    public KocReviewTaskPage SentPage { get; private set; } = new([], 0, 1, 10, 1, 0, 0);
    public IReadOnlyList<ReceivedReviewItem> ReceivedItems { get; private set; } = [];
    public ReceivedReviewPage ReceivedPage { get; private set; } = new([], 0, 1, 10, 1, 0, 0, 0);
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        SentPage = await reviewService.GetKocReviewTaskPageAsync(
            UserId(),
            new KocReviewTaskQuery(SentSearch, SentFromDate, SentToDate, SentRating, SentState, SentPageNumber, SentPageSize),
            cancellationToken);
        Items = SentPage.Items;
        ReceivedPage = await reviewService.GetReceivedReviewPageAsync(
            UserId(),
            new ReceivedReviewQuery(Search, FromDate, ToDate, Rating, State, PageNumber, PageSize),
            cancellationToken);
        ReceivedItems = ReceivedPage.Items;
        return Page();
    }

    public async Task<IActionResult> OnGetReceivedDataAsync(
        int draw = 1, int start = 0, int length = 10, string? search = null,
        DateTime? fromDate = null, DateTime? toDate = null, byte? rating = null,
        string? state = null, CancellationToken cancellationToken = default)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return new JsonResult(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Hồ sơ chưa đủ điều kiện sử dụng tính năng." }) { StatusCode = StatusCodes.Status403Forbidden };
        length = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length);
        var page = start / length + 1;
        var result = await reviewService.GetReceivedReviewPageAsync(
            UserId(), new ReceivedReviewQuery(search, fromDate, toDate, rating, state, page, length), cancellationToken);
        return new JsonResult(new
        {
            draw,
            recordsTotal = result.AllCount,
            recordsFiltered = result.TotalCount,
            data = result.Items.Select(x => new
            {
                id = x.Id,
                x.CampaignTitle,
                partnerName = x.ReviewerName,
                x.Rating,
                x.Comment,
                x.Status,
                x.CreatedAtUtc,
                x.ReplyBody,
                x.RepliedAtUtc,
                x.AppealStatus,
                x.AppealReason,
                x.AppealDecisionNote
            })
        });
    }

    public async Task<IActionResult> OnGetSentDataAsync(
        int draw = 1, int start = 0, int length = 10, string? search = null,
        DateTime? fromDate = null, DateTime? toDate = null, byte? rating = null,
        string? state = null, CancellationToken cancellationToken = default)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return new JsonResult(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Hồ sơ chưa đủ điều kiện sử dụng tính năng." }) { StatusCode = StatusCodes.Status403Forbidden };
        length = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length);
        var page = start / length + 1;
        var result = await reviewService.GetKocReviewTaskPageAsync(
            UserId(), new KocReviewTaskQuery(search, fromDate, toDate, rating, state, page, length), cancellationToken);
        var editHours=(await setup.GetPolicyAsync(cancellationToken)).ReviewEditHours;
        return new JsonResult(new
        {
            draw,
            recordsTotal = result.PendingCount + result.CompletedCount,
            recordsFiltered = result.TotalCount,
            data = result.Items.Select(x => new
            {
                participationId = x.ParticipationId,
                campaignId = x.CampaignId,
                x.CampaignTitle,
                partnerName = x.BrandName,
                x.IsReviewed,
                x.Rating,
                x.Comment,
                x.ReviewedAtUtc,
                canEdit = x.IsReviewed && x.ReviewedAtUtc.HasValue && x.ReviewedAtUtc.Value.AddHours(editHours) > DateTime.UtcNow
            })
        });
    }

    public async Task<IActionResult> OnPostReplyAsync(Guid id, string body, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await reviewService.ReplyAsync(UserId(), id, body, cancellationToken);
        if (IsAjax()) return new JsonResult(new { success = result.Success, message = result.Message });
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { tab = "received" });
    }

    public async Task<IActionResult> OnPostAppealAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await reviewService.AppealAsync(UserId(), id, reason, cancellationToken);
        if (IsAjax()) return new JsonResult(new { success = result.Success, message = result.Message });
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { tab = "received" });
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, byte rating, string? comment, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await reviewService.ReviewBrandAsync(UserId(), id, rating, comment, cancellationToken);
        if (IsAjax()) return new JsonResult(new { success = result.Success, message = result.Message });
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { tab = "sent" });
    }

    public async Task<IActionResult> OnPostUpdateReviewAsync(Guid id, byte rating, string? comment, string reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await reviewService.UpdateBrandReviewAsync(UserId(), id, rating, comment, reason, cancellationToken);
        if (IsAjax()) return new JsonResult(new { success = result.Success, message = result.Message });
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage(new { tab = "sent" });
    }

    public async Task<IActionResult> OnPostSupplementAppealAsync(Guid id, string body, CancellationToken cancellationToken)
    {
        return await RunAppealActionAsync(
            () => reviewService.SupplementAppealAsync(UserId(), id, body, cancellationToken),
            "bổ sung yêu cầu xem xét");
    }

    public async Task<IActionResult> OnPostWithdrawAppealAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        return await RunAppealActionAsync(
            () => reviewService.WithdrawAppealAsync(UserId(), id, reason, cancellationToken),
            "rút yêu cầu xem xét");
    }

    private async Task<IActionResult> RunAppealActionAsync(Func<Task<ReviewResult>> action, string actionName)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null)
        {
            return IsAjax()
                ? new JsonResult(new { success = false, message = "Hồ sơ KOC chưa đủ điều kiện thực hiện thao tác này." }) { StatusCode = StatusCodes.Status403Forbidden }
                : redirect;
        }

        try
        {
            var result = await action();
            if (IsAjax()) return new JsonResult(new { success = result.Success, message = result.Message });
            Message = result.Message;
            MessageSuccess = result.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không thể {ActionName} cho KOC {UserId}.", actionName, UserId());
            const string message = "Hệ thống chưa thể lưu thay đổi. Vui lòng tải lại trang và thử lại.";
            if (IsAjax()) return new JsonResult(new { success = false, message }) { StatusCode = StatusCodes.Status500InternalServerError };
            Message = message;
            MessageSuccess = false;
        }

        return RedirectToPage(new { tab = "received" });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAjax() => string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
