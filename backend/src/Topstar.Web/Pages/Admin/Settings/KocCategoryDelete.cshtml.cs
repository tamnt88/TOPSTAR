using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class KocCategoryDeleteModel(IKocCategoryAdminService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public KocCategoryAdminItem Item { get; private set; } = null!;


    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        => await LoadAsync(cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken)) return NotFound();
        if (!ModelState.IsValid) return Page();
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await service.DeleteAsync(actor, Id, "Xóa lĩnh vực nội dung KOC chưa sử dụng.", cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }
        TempData["Message"] = result.Message;
        TempData["Success"] = true;
        return RedirectToPage("KocCategories");
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(Id, cancellationToken);
        if (item is null) return false;
        Item = item;
        return true;
    }
}
