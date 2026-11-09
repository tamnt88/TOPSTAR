using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Topstar.Application.SystemSettings;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class SeoSocialDeleteModel(ISystemSettingsAdminService service):PageModel
{
 public SeoConfigItem? Item{get;private set;}
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken token){Item=await service.GetSeoConfigByIdAsync(id,token);return Item is null?NotFound():Page();}
 public async Task<IActionResult> OnPostAsync(Guid id,CancellationToken token){var result=await service.DeleteSeoConfigAsync(id,token);TempData[result.Success?"AdminMessageSuccess":"AdminMessageError"]=result.Message;return RedirectToPage("SeoSocial");}
}
