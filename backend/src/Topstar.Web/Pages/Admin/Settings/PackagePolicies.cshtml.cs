using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class PackagePoliciesModel(IPackagePolicyAdminService service, ILogger<PackagePoliciesModel> logger) : PageModel
{
    public void OnGet() { }

    public async Task<JsonResult> OnGetDataAsync(CancellationToken cancellationToken)
    {
        var items = await service.GetAllAsync(cancellationToken);
        return new JsonResult(new
        {
            data = items.Select(x => new
            {
                x.Id, x.Code, x.Name, x.ReferenceBudget, x.SalePrice, x.ImageUrl, x.Description, x.Benefits, x.ValidityDays,
                x.IsFeatured, x.DisplayOrder, x.MinBudgetAmount, x.MaxBudgetAmount,
                x.SelectionQuota, x.BrandSelectionEnabled, x.IsActive
            })
        });
    }

    public async Task<JsonResult> OnPostDeleteAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            var admin = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.DeleteAsync(admin, id, reason, cancellationToken);
            Response.StatusCode = result.Success ? 200 : 400;
            return new JsonResult(new { success = result.Success, message = result.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Admin failed to soft-delete package policy {PolicyId}", id);
            Response.StatusCode = 500;
            return new JsonResult(new { success = false, message = "Hệ thống không thể xóa gói KOC. Lỗi đã được ghi nhật ký." });
        }
    }
}
