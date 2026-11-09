using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Profiles;
using Topstar.Application.Shipments;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Koc;

public sealed class ShipmentsModel(IProfileAccessService profileAccessService, IShipmentWorkflowService service, IFileStorageService fileStorage) : KocGuardedPageModel(profileAccessService)
{
    public Topstar.Application.Common.ListWindow Window { get; private set; } = new();
    public IReadOnlyList<ShipmentWorkItem> Items { get; private set; } = [];
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect; Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"kocShipmentTable"); Items = await service.GetKocItemsAsync(UserId(), ct, Window); return Page(); }
    public async Task<JsonResult> OnGetDataAsync(int draw, int start, int length, string? query, string? status, CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) { Response.StatusCode = 403; return new JsonResult(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Hồ sơ chưa đủ điều kiện." }); }
        length = Topstar.Core.Common.Pagination.PaginationDefaults.Normalize(length);
        var window = new Topstar.Application.Common.ListWindow { Page = Math.Max(0,start)/length+1, Size=length, Search=query, Status=status };
        var rows = await service.GetKocItemsAsync(UserId(),ct,window);
        return new JsonResult(new { draw=Math.Max(0,draw),recordsTotal=window.Total,recordsFiltered=window.Filtered,data=rows });
    }
    public async Task<IActionResult> OnPostDeliveredAsync(Guid id, IFormFile? receiptFile, string? receiptNote, CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        if (!ShipmentEvidenceRules.IsValidReceiptNote(receiptNote))
            return Done(new(false, "Vui lòng nhập ghi chú nhận hàng từ 10 đến 500 ký tự."));
        var url = await fileStorage.SaveEvidenceImageAsync(receiptFile, ct);
        if (url is null) return Done(new(false, "Vui lòng chọn ảnh JPG, PNG hoặc WEBP hợp lệ và không quá 5 MB."));
        ShipmentResult result;
        try { result = await service.ConfirmDeliveredAsync(UserId(), id, url, receiptNote!.Trim(), ct); }
        catch (ShipmentConcurrencyException ex) { result = new(false, ex.Message); }
        catch
        {
            await fileStorage.DeleteEvidenceImageAsync(url, ct);
            throw;
        }
        if (!result.Success) await fileStorage.DeleteEvidenceImageAsync(url, ct);
        return Done(result);
    }
    public Task<IActionResult> OnPostIssueAsync(Guid id, string issueNote, CancellationToken ct) => RunAsync(() => service.ReportIssueAsync(UserId(), id, issueNote, ct));
    public async Task<IActionResult> OnPostEvidenceAsync(Guid id, IFormFile? evidenceFile, string? caption, CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        if (!ShipmentEvidenceRules.IsValidEvidenceCaption(caption))
            return Done(new(false, "Vui lòng nhập ghi chú ảnh từ 10 đến 500 ký tự."));
        var url = await fileStorage.SaveEvidenceImageAsync(evidenceFile, ct);
        if (url is null) return Done(new(false, "Ảnh phải là JPG, PNG hoặc WEBP hợp lệ và không quá 5 MB."));
        ShipmentResult result;
        try { result = await service.AddEvidenceAsync(UserId(), id, "KOC", url, caption, ct); }
        catch
        {
            await fileStorage.DeleteEvidenceImageAsync(url, ct);
            throw;
        }
        if (!result.Success) await fileStorage.DeleteEvidenceImageAsync(url, ct);
        return Done(result);
    }
    private async Task<IActionResult> RunAsync(Func<Task<ShipmentResult>> action) { var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect; try { return Done(await action()); } catch (ShipmentConcurrencyException ex) { return Done(new(false, ex.Message)); } }
    private IActionResult Done(ShipmentResult result) { Message = result.Message; MessageSuccess = result.Success; return RedirectToListState(); }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
