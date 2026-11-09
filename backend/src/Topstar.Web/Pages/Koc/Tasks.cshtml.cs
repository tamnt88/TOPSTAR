using Topstar.Core.Common.Pagination;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Deliverables;
using Topstar.Application.Profiles;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Koc;

public sealed class TasksModel(
    IProfileAccessService profileAccessService,
    IDeliverableWorkflowService workflowService,
    IFileStorageService fileStorage) : KocGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Due { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; } = "due";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;

    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        PageSize = PaginationDefaults.Normalize(PageSize);
        return Page();
    }

    public async Task<JsonResult> OnGetDataAsync(
        int draw,
        int start,
        int length,
        string? query,
        string? status,
        string? due,
        string? sort,
        CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return new JsonResult(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Hồ sơ chưa đủ điều kiện sử dụng tính năng." });
        }
        length = PaginationDefaults.Normalize(length);
        start = Math.Max(0, start);
        var page = start / length + 1;
        var result = await workflowService.GetKocTaskPageAsync(
            UserId(),
            new(query, status, due, sort, page, length),
            cancellationToken);
        return new JsonResult(new
        {
            draw = Math.Max(0, draw),
            recordsTotal = result.UnfilteredCount,
            recordsFiltered = result.TotalCount,
            data = result.Items,
            summary = new { result.NotSubmittedCount, result.NeedRevisionCount, result.SubmittedCount, result.ApprovedCount, result.OverdueCount }
        });
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, string? submissionUrl, string? note, IFormFile? attachmentFile, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if (string.IsNullOrWhiteSpace(note))
            return JsonOrRedirect(new(false, "Ghi chú cho Brand là bắt buộc."));
        string? attachmentUrl = null;
        if (attachmentFile is { Length: > 0 })
        {
            attachmentUrl = await fileStorage.SaveSubmissionAttachmentAsync(attachmentFile, cancellationToken);
            if (attachmentUrl is null)
                return JsonOrRedirect(new(false, "Tệp không hợp lệ. Hỗ trợ JPG, PNG, WEBP, PDF, DOC, DOCX, MP4, MOV; tối đa 25 MB."));
        }
        var result = await workflowService.SubmitAsync(UserId(), id, submissionUrl, note, attachmentUrl, attachmentFile?.FileName, cancellationToken);
        if (!result.Success && attachmentUrl is not null)
            await fileStorage.DeleteSubmissionAttachmentAsync(attachmentUrl, cancellationToken);
        return JsonOrRedirect(result);
    }

    public async Task<IActionResult> OnPostRedactSensitiveDataAsync(Guid historyId, bool redactLink, bool redactNote, bool redactAttachment, string? reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await workflowService.RedactSensitiveDataAsync(UserId(), historyId, redactLink, redactNote, redactAttachment, reason, cancellationToken);
        if (result.Success && result.StoredUrl is not null)
            await fileStorage.DeleteSubmissionAttachmentAsync(result.StoredUrl, cancellationToken);
        return JsonOrRedirect(new(result.Success, result.Message));
    }

    private IActionResult JsonOrRedirect(DeliverableResult result)
    {
        if (string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return new JsonResult(new { success = result.Success, message = result.Message });
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
