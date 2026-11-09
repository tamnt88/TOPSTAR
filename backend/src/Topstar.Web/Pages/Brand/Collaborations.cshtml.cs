using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Collaborations;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class CollaborationsModel(IProfileAccessService profileAccessService,ICollaborationHistoryService service):BrandGuardedPageModel(profileAccessService)
{
 public Topstar.Application.Common.ListWindow Window { get; private set; } = new();

 [BindProperty(SupportsGet=true)]public Guid? CampaignId{get;set;}
 public IReadOnlyList<CollaborationItem> Items{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){ Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandCollaborationTable");var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;Items=await service.GetForBrandAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),CampaignId,ct, Window);return Page();}
}
