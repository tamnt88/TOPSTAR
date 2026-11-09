using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class KocProfilePreviewModel(IProfileReviewService profileReviewService, TopstarDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public ProfileReviewDetail Profile { get; private set; } = null!;
    public Guid UserId { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var profile = await profileReviewService.GetDetailAsync("KOC", Id, cancellationToken);
        if (profile?.KocDetail is null) return NotFound();
        UserId = await db.KocProfiles.AsNoTracking().Where(x => x.Id == Id).Select(x => x.UserId).FirstOrDefaultAsync(cancellationToken);
        if (UserId == Guid.Empty) return NotFound();
        Profile = profile;
        return Page();
    }
}
