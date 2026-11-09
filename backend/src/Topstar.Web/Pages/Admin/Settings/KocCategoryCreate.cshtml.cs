using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class KocCategoryCreateModel(IKocCategoryAdminService service) : PageModel
{
    [BindProperty] public KocCategoryFormInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await service.GetAllAsync(cancellationToken);
        Input.SortOrder = items.Count == 0 ? 10 : items.Max(x => x.SortOrder) + 10;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await service.SaveAsync(actor, new(null, Input.Code, Input.Name, Input.SortOrder, "Thêm lĩnh vực nội dung KOC."), cancellationToken);
        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.Field)) ModelState.AddModelError($"Input.{result.Field}", result.Message);
            else ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }
        TempData["Message"] = result.Message;
        TempData["Success"] = true;
        return RedirectToPage("KocCategories");
    }
}
