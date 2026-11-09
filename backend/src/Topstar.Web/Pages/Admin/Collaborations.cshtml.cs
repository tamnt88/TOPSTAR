using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Collaborations;

namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class CollaborationsModel(ICollaborationHistoryService service):PageModel
{
 public Topstar.Application.Common.ListWindow Window { get; private set; } = new();

 public IReadOnlyList<CollaborationItem> Items{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct) { Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"adminCollaborationList"); Items=await service.GetForAdminAsync(ct, Window); }
}
