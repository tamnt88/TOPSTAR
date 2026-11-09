using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class SeoSocialModel(ISystemSettingsAdminService service):PageModel
{
 public IReadOnlyList<SeoConfigItem> Rows{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken token)=>Rows=await service.GetSeoConfigsAsync(token);
}
