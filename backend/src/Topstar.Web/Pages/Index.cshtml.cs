using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Website;

namespace Topstar.Web.Pages;

public class IndexModel(IHomepageContentService homepage) : PageModel
{
    public HomepageContent Homepage { get; private set; } = HomepageContent.Default;
    public async Task OnGetAsync()
    {
        var current = await homepage.GetAsync(HttpContext.RequestAborted);
        Homepage = current.IsPublished ? current : HomepageContent.Default;
    }
}
