using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Topstar.Application.Campaigns;
using Topstar.Application.Profiles;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class CampaignsModel(IProfileAccessService profileAccessService, ICampaignService campaignService) : BrandGuardedPageModel(profileAccessService)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        return redirect ?? Page();
    }

    public async Task<IActionResult> OnGetDataAsync(int draw, int start, int length, string? keyword, string? status, int orderColumn, string? orderDirection, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        if (redirect is not null)
            return new JsonResult(new { data = Array.Empty<object>(), message = "Hồ sơ Brand chưa đủ điều kiện sử dụng chức năng này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        keyword = keyword?.Trim();
        if (keyword?.Length > 120) keyword = keyword[..120];
        status = status is CampaignStatuses.Draft or CampaignStatuses.Submitted or CampaignStatuses.NeedEdit or CampaignStatuses.Published
            or CampaignStatuses.InProgress or CampaignStatuses.Completed or CampaignStatuses.Rejected or CampaignStatuses.Cancelled ? status : null;
        var result = await campaignService.GetBrandCampaignPageAsync(
            GetUserId(),
            new(keyword, status, start, length, orderColumn, string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase)),
            cancellationToken);
        return new JsonResult(new
        {
            draw = Math.Max(0, draw),
            recordsTotal = result.TotalCount,
            recordsFiltered = result.FilteredCount,
            data = result.Items.Select((item, index) => new
            {
                item.Id,
                RowNumber = Math.Max(0, start) + index + 1,
                item.Title,
                item.AvatarUrl,
                item.BannerUrl,
                item.SelectedKocCount,
                item.DesiredKocCount,
                item.FundedBudget,
                item.CanEditImages,
                item.HasWorkStarted,
                item.BudgetAmount,
                item.PackageCode,
                item.Status,
                item.IsShortlistFinalized,
                StartDate = item.StartDate?.ToString("dd/MM/yyyy"),
                EndDate = item.EndDate?.ToString("dd/MM/yyyy"),
                item.ApplicationCount,
                CanEdit = CampaignTransitions.CanEdit(item.Status),
                CanSubmit = CampaignTransitions.CanSubmit(item.Status),
                CanShortlist = item.Status == CampaignStatuses.Published,
                CanReviewContent = item.Status is CampaignStatuses.InProgress or CampaignStatuses.Completed,
                CanReviewKoc = item.Status == CampaignStatuses.Completed
            }),
            summary = new
            {
                result.Summary.Draft,
                result.Summary.Review,
                result.Summary.Running,
                result.Summary.Completed
            }
        });
    }

    public async Task<IActionResult> OnPostImagesAsync(Guid id,IFormFile? avatarFile,IFormFile? bannerFile,CancellationToken cancellationToken)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);
        if(redirect is not null)return new JsonResult(new{success=false,message="Hồ sơ Brand chưa đủ điều kiện."}){StatusCode=403};
        var storage=HttpContext.RequestServices.GetRequiredService<IFileStorageService>();
        string? avatar=null,banner=null;
        var committed=false;
        try
        {
            if(avatarFile is {Length:>0}){avatar=await storage.SaveCampaignImageAsync(avatarFile,cancellationToken);if(avatar is null)return new JsonResult(new{success=false,message="Ảnh đại diện phải là JPG, PNG hoặc WEBP, tối đa 5 MB."}){StatusCode=400};}
            if(bannerFile is {Length:>0}){banner=await storage.SaveCampaignImageAsync(bannerFile,cancellationToken);if(banner is null)return new JsonResult(new{success=false,message="Banner phải là JPG, PNG hoặc WEBP, tối đa 5 MB."}){StatusCode=400};}
            var result=await campaignService.UpdateImagesAsync(id,GetUserId(),avatar,banner,cancellationToken);
            committed=result.Success;
            return new JsonResult(new{success=result.Success,message=result.Message}){StatusCode=result.Success?200:400};
        }
        finally
        {
            if(!committed){await storage.DeleteCampaignImageAsync(avatar,CancellationToken.None);await storage.DeleteCampaignImageAsync(banner,CancellationToken.None);}
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        if (redirect is not null)
            return new JsonResult(new { success = false, message = "Hồ sơ Brand chưa đủ điều kiện sử dụng chức năng này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        var result = await campaignService.SubmitAsync(id, GetUserId(), cancellationToken);
        return new JsonResult(new { success = result.Success, message = result.Message })
        {
            StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
