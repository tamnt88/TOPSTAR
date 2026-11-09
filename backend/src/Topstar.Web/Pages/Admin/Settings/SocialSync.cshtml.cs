using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SocialSync;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class SocialSyncModel(ISocialSyncOrchestrator socialSyncOrchestrator):PageModel{
 [TempData]public string? SyncMessage{get;set;}
 [BindProperty]public Guid? KocProfileId{get;set;}
 public IReadOnlyList<AdminSocialSyncKocItem> SyncKocs{get;private set;}=[];
 public IReadOnlyList<AdminSocialSyncJobItem> SyncJobs{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct){SyncKocs=await socialSyncOrchestrator.GetAdminKocsAsync(ct);SyncJobs=await socialSyncOrchestrator.GetAdminRecentJobsAsync(30,ct);}
    public async Task<IActionResult> OnPostSyncAllKocAsync(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var result = await socialSyncOrchestrator.RequestAdminSyncAllKocsAsync(userId, cancellationToken);
        SyncMessage = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSyncOneKocAsync(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        if (!KocProfileId.HasValue || KocProfileId.Value == Guid.Empty)
        {
            SyncMessage = "Vui lòng nhập KOC Profile ID.";
            return RedirectToPage();
        }

        var result = await socialSyncOrchestrator.RequestAdminSyncSingleKocAsync(userId, KocProfileId.Value, cancellationToken);
        SyncMessage = result.Message;
        return RedirectToPage();
    }
}