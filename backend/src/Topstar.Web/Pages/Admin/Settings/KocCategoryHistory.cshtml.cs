using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class KocCategoryHistoryModel(IKocCategoryAdminService service) : PageModel
{
    public IReadOnlyList<KocCategoryHistoryItem> Items { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken) => Items = await service.GetHistoryAsync(cancellationToken);
}
