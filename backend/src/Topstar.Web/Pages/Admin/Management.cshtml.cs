using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class ManagementModel : PageModel
{
    public void OnGet()
    {
    }
}
