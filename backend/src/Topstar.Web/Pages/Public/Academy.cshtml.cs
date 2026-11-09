using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Academy;
namespace Topstar.Web.Pages.Public;
public sealed class AcademyModel(IAcademyService service, IAcademyLandingService landingService):PageModel
{
 public IReadOnlyList<AcademyCourseItem> Courses{get;private set;}=[];
 public AcademyLandingContent LandingContent{get;private set;}=AcademyLandingContent.Default;
 public async Task OnGetAsync(CancellationToken ct)
 {
  Courses=await service.GetPublishedAsync(ct);
  var content=await landingService.GetAsync(ct);
  LandingContent=content.IsPublished?content:AcademyLandingContent.Default;
 }
}
