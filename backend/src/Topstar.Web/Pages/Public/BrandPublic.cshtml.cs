using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Website;

namespace Topstar.Web.Pages.Public;

public sealed class BrandPublicModel(IHomepageContentService homepage) : PageModel
{
    public IReadOnlyList<HomepagePartnerItem> Partners { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var content = await homepage.GetAsync(HttpContext.RequestAborted);
        if (!content.IsPublished) content = HomepageContent.Default;
        Partners = content.Partners.Where(x => x.IsVisible).OrderBy(x => x.DisplayOrder).ToArray();
    }
}
