using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class MatchingDetailModel(IMatchingService matchingService) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public List<Guid> SelectedApplicationIds { get; set; } = [];
    [BindProperty] public List<Guid> SelectedKocProfileIds { get; set; } = [];
    [BindProperty, StringLength(500)] public string? Note { get; set; }
    public AdminMatchingDetail? Data { get; private set; }
    [TempData] public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Data = await matchingService.GetAdminDetailAsync(Id, cancellationToken);
        return Data is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await matchingService.SendShortlistAsync(adminUserId, Id, SelectedApplicationIds, Note, cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); Data = await matchingService.GetAdminDetailAsync(Id, cancellationToken); return Page(); }
        Message = result.Message;
        return RedirectToPage("/Admin/MatchingDetail", new { id = Id });
    }

    public async Task<IActionResult> OnPostAssignDirectAsync(CancellationToken cancellationToken)
    {
        var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await matchingService.AssignDirectAsync(adminUserId, Id, SelectedKocProfileIds, Note, cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); Data = await matchingService.GetAdminDetailAsync(Id, cancellationToken); return Page(); }
        Message = result.Message;
        return RedirectToPage("/Admin/MatchingDetail", new { id = Id });
    }
}
