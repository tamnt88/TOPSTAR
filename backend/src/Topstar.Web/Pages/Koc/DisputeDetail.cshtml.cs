using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Disputes;
using Topstar.Application.Profiles;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Koc;

public sealed class DisputeDetailModel(IProfileAccessService access, IDisputeService service, IFileStorageService fileStorage) : KocGuardedPageModel(access)
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public DisputeCaseDetail? Detail { get; private set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        Detail = await service.GetDetailAsync(UserId(), "KOC", Id, cancellationToken);
        return Detail is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostMessageAsync(string body, string? evidenceUrl, IFormFile? evidenceFile, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if (evidenceFile is not null && !string.IsNullOrWhiteSpace(evidenceUrl))
        {
            Message = "Chỉ chọn một hình thức bằng chứng: tải tệp hoặc nhập liên kết.";
            MessageSuccess = false;
            return RedirectToPage(new { id = Id });
        }
        string? storedUrl = null;
        if (evidenceFile is not null)
        {
            storedUrl = await fileStorage.SaveDisputeEvidenceAsync(evidenceFile, cancellationToken);
            if (storedUrl is null)
            {
                Message = "Bằng chứng phải là JPG, PNG, WebP hoặc PDF hợp lệ và không vượt quá 5 MB.";
                MessageSuccess = false;
                return RedirectToPage(new { id = Id });
            }
        }
        var result = await service.AddMessageAsync(UserId(), "KOC", Id, body, storedUrl ?? evidenceUrl, false, cancellationToken);
        if (!result.Success && storedUrl is not null) await fileStorage.DeleteDisputeEvidenceAsync(storedUrl, cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage(new { id = Id });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
