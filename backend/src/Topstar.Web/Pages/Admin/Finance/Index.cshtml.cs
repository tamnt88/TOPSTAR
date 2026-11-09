using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Topstar.Web.Pages.Admin.Finance;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
