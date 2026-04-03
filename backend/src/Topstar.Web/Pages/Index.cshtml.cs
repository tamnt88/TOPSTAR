using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Topstar.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}
