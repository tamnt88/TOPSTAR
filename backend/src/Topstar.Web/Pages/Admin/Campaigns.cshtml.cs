using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Topstar.Application.Campaigns;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class CampaignsModel(ICampaignService campaignService) : PageModel
{
    [Microsoft.AspNetCore.Mvc.TempData] public string? Message { get; set; }
    [Microsoft.AspNetCore.Mvc.TempData] public bool MessageSuccess { get; set; }
    public void OnGet() { }

    public async Task<IActionResult> OnGetDataAsync(int draw, int start, int length, string? keyword, string? status, int orderColumn, string? orderDirection, CancellationToken cancellationToken)
    {
        keyword = keyword?.Trim();
        if (keyword?.Length > 120) keyword = keyword[..120];
        status = status is CampaignStatuses.Draft or CampaignStatuses.Submitted or CampaignStatuses.NeedEdit or CampaignStatuses.Published
            or CampaignStatuses.InProgress or CampaignStatuses.Completed or CampaignStatuses.Rejected or CampaignStatuses.Cancelled or "Active" or "Attention" ? status : null;
        var result = await campaignService.GetAdminCampaignPageAsync(new(keyword, status, start, length, orderColumn,
            string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase)), cancellationToken);
        return new JsonResult(new
        {
            draw = Math.Max(0, draw), recordsTotal = result.TotalCount, recordsFiltered = result.FilteredCount,
            data = result.Items.Select((item, index) => new
            {
                item.Id, RowNumber = Math.Max(0, start) + index + 1, item.Title, item.BrandName, item.BudgetAmount,
                item.PackageCode, item.Status, StartDate = item.StartDate?.ToString("dd/MM/yyyy"), EndDate = item.EndDate?.ToString("dd/MM/yyyy"), item.ApplicationCount
            }),
            summary = result.Summary
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId)) return Unauthorized();
        try
        {
            var result = await campaignService.DeleteByAdminAsync(id, adminUserId, reason, cancellationToken);
            Message = result.Message;
            MessageSuccess = result.Success;
        }
        catch
        {
            Message = "Không thể xóa chiến dịch vì còn dữ liệu ràng buộc chưa được xử lý. Hệ thống đã hoàn tác toàn bộ thay đổi.";
            MessageSuccess = false;
        }
        return RedirectToPage();
    }
}
