using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Public;

public sealed class ContentModel(TopstarDbContext db) : PageModel
{
    public PublicPageSeoConfigEntity? Item { get; private set; }
    public async Task<IActionResult> OnGetAsync(string? pagePath, CancellationToken ct)
    {
        var path = "/" + (pagePath ?? "").Trim('/');
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Brand", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Koc", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)) return NotFound();
        try
        {
            Item = await db.PublicPageSeoConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.PagePath == path && x.IsActive && x.IsVisible, ct);
            if (Item is null || string.IsNullOrWhiteSpace(Item.PageTitle)) return NotFound();
            await db.PublicPageSeoConfigs.Where(x => x.Id == Item.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);
            return Page();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 207 or 208) { return NotFound(); }
    }
}
