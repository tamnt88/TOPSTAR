using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Shipments;
using Topstar.Web.Services.Storage;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Brand;

public sealed class ShipmentsModel(IProfileAccessService profileAccessService, IShipmentWorkflowService service, IFileStorageService fileStorage,IOperationalSetupService setup) : BrandGuardedPageModel(profileAccessService)
{
 public Topstar.Application.Common.ListWindow Window { get; private set; } = new();

    public IReadOnlyList<ShipmentWorkItem> Items { get; private set; } = [];
    public IReadOnlyList<OperationalCatalogItem> Checkpoints { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public Guid? CampaignId { get; set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandShipmentTable"); var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect; Items = CampaignId.HasValue ? await service.GetBrandItemsForCampaignAsync(UserId(), CampaignId.Value, ct, Window) : await service.GetBrandItemsAsync(UserId(), ct, Window);Checkpoints=await setup.GetCatalogAsync(OperationalCatalogTypes.ShipmentCheckpoint,true,ct); return Page(); }
    public Task<IActionResult> OnPostCreateAsync(Guid participationId, CancellationToken ct) => RunAsync(() => service.CreateAsync(UserId(), participationId, ct));
    public Task<IActionResult> OnPostDispatchAsync(Guid id, string carrier, string trackingCode, CancellationToken ct) => RunAsync(() => service.DispatchAsync(UserId(), id, carrier, trackingCode, ct));
    public Task<IActionResult> OnPostResolveAsync(Guid id, string decision, string note, CancellationToken ct) => RunAsync(() => service.ResolveIssueAsync(UserId(), id, decision, note, ct));
    public Task<IActionResult> OnPostTrackingAsync(Guid id, string eventCode, string description, string? location, DateTime occurredAtLocal, CancellationToken ct)
        => RunAsync(() => service.AddTrackingEventAsync(UserId(), id, eventCode, description, location, occurredAtLocal, ct));
    public async Task<IActionResult> OnPostEvidenceAsync(Guid id, IFormFile? evidenceFile, string? caption, CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var url = await fileStorage.SaveEvidenceImageAsync(evidenceFile, ct);
        if (url is null) return Done(new(false, "Ảnh phải là JPG, PNG hoặc WEBP hợp lệ và không quá 5 MB."));
        var result = await service.AddEvidenceAsync(UserId(), id, "BRAND", url, caption, ct);
        if (!result.Success) await fileStorage.DeleteEvidenceImageAsync(url, ct);
        return Done(result);
    }
    private async Task<IActionResult> RunAsync(Func<Task<ShipmentResult>> action) { var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect; try { return Done(await action()); } catch (ShipmentConcurrencyException ex) { return Done(new(false, ex.Message)); } }
    private IActionResult Done(ShipmentResult result) { Message = result.Message; MessageSuccess = result.Success; return RedirectToPage(new { campaignId = CampaignId }); }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
