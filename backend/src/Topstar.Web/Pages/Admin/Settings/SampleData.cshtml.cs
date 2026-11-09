using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class SampleDataModel(ISampleDataCleanupService service) : PageModel
{
    public const string ConfirmationText = "XOA DU LIEU MAU";
    public SampleDataCleanupPreview Preview { get; private set; } = new(0, 0, 0, 0, 0, 0);

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập câu xác nhận.")]
    public string Confirmation { get; set; } = string.Empty;

    [TempData] public string? Message { get; set; }
    [TempData] public bool Success { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => Preview = await service.PreviewAsync(UserId(), ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!string.Equals(Confirmation.Trim(), ConfirmationText, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(Confirmation), $"Hãy nhập chính xác: {ConfirmationText}");

        if (!ModelState.IsValid)
        {
            Preview = await service.PreviewAsync(UserId(), ct);
            return Page();
        }

        var result = await service.DeleteAsync(UserId(), ct);
        Message = result.Message;
        Success = result.Success;
        return RedirectToPage();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
