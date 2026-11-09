using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class KocCategoriesModel(IKocCategoryAdminService service) : PageModel
{
    [TempData] public string? Message { get; set; }
    [TempData] public bool Success { get; set; }
    public void OnGet() { }

    public async Task<IActionResult> OnGetDataAsync(CancellationToken cancellationToken)
    {
        var items = await service.GetAllAsync(cancellationToken);
        return new JsonResult(new { data = items });
    }

    public async Task<IActionResult> OnPostSetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SetActiveAsync(CurrentUserId(), id, isActive, isActive ? "Hiển thị lĩnh vực nội dung KOC." : "Tạm ẩn lĩnh vực nội dung KOC.", cancellationToken);
            return new JsonResult(result) { StatusCode = result.Success ? 200 : 400 };
        }
        catch
        {
            return StatusCode(500, new KocCategoryAdminResult(false, "Không thể cập nhật trạng thái lúc này. Vui lòng thử lại.", id));
        }
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
