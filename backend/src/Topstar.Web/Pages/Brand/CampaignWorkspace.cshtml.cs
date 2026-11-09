using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Campaigns;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

public sealed class CampaignWorkspaceModel(IProfileAccessService access, ICampaignWorkspaceService workspaceService, Topstar.Web.Services.Email.ICampaignParticipationEmailService participationEmail, Topstar.Infrastructure.Campaigns.CampaignLifecycleService lifecycle, ILogger<CampaignWorkspaceModel> logger) : BrandGuardedPageModel(access)
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public CampaignWorkspace? Data { get; private set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool Success { get; set; }

    public async Task<IActionResult> OnPostLifecycleAsync(string action,string? reason,CancellationToken ct)
    {
        try
        {
            var result=await lifecycle.ChangeAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),Id,action,reason,ct);
            Message=result.Message;Success=result.Success;
            if(result.Changed)
                try { await participationEmail.SendLifecycleAsync(Id,action,reason??"",ct); }
                catch(Exception ex) when(ex is not OperationCanceledException) { logger.LogError(ex,"Could not send campaign lifecycle email for {CampaignId}",Id); }
        }
        catch(Microsoft.Data.SqlClient.SqlException ex) when(ex.Number==1205)
        { Message="Có thao tác khác đang xử lý. Vui lòng tải lại và thử lại; chưa ghi nhận thay đổi này.";Success=false; }
        catch(Microsoft.EntityFrameworkCore.DbUpdateException ex) when(ex.InnerException is Microsoft.Data.SqlClient.SqlException {Number:1205})
        { Message="Có thao tác khác đang xử lý. Vui lòng tải lại và thử lại; chưa ghi nhận thay đổi này.";Success=false; }
        return RedirectToPage(new{id=Id});
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if (Id == Guid.Empty) return RedirectToPage("/Brand/Campaigns");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Data = await workspaceService.GetForBrandAsync(userId, Id, cancellationToken);
        return Data is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostOpenWorkAsync(Guid participationId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await workspaceService.OpenWorkAsync(userId, Id, participationId, false, cancellationToken);
        Message = result.Message; Success = result.Success;
        if (result.Success)
            foreach (var id in result.ParticipationIds) await participationEmail.SendWorkOpenedAsync(id, cancellationToken);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostOpenAcceptedAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await workspaceService.OpenWorkAsync(userId, Id, null, false, cancellationToken);
        Message = result.Message; Success = result.Success;
        if (result.Success)
            foreach (var id in result.ParticipationIds) await participationEmail.SendWorkOpenedAsync(id, cancellationToken);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostOpenFullTeamAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await workspaceService.OpenWorkAsync(userId, Id, null, true, cancellationToken);
        Message = result.Message; Success = result.Success;
        if (result.Success)
            foreach (var id in result.ParticipationIds) await participationEmail.SendWorkOpenedAsync(id, cancellationToken);
        return RedirectToPage(new { id = Id });
    }
}
