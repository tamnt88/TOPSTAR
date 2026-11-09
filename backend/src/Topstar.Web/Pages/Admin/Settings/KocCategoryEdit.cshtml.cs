using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class KocCategoryEditModel(IKocCategoryAdminService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public KocCategoryFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(Id, cancellationToken);
        if (item is null) return NotFound();
        Input = new() { Name = item.Name, Code = item.Code, SortOrder = item.SortOrder };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await service.SaveAsync(actor, new(Id, Input.Code, Input.Name, Input.SortOrder, "Cập nhật lĩnh vực nội dung KOC."), cancellationToken);
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
