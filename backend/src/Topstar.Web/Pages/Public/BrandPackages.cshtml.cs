using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;
namespace Topstar.Web.Pages.Public;
public sealed class BrandPackagesModel(IPackagePolicyAdminService service):PageModel
{
 public IReadOnlyList<PackagePolicyItem> Items{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct)=>Items=await service.GetPublicAsync(ct);
}
