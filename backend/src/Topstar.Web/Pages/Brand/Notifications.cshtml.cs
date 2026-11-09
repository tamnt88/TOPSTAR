using System.Security.Claims;using Microsoft.AspNetCore.Mvc;using Topstar.Application.Notifications;using Topstar.Application.Profiles;
namespace Topstar.Web.Pages.Brand;
public sealed class NotificationsModel(IProfileAccessService access,INotificationInboxService service):BrandGuardedPageModel(access)
{
 [BindProperty(SupportsGet=true)]public string Filter{get;set;}="all";public NotificationInbox Inbox{get;private set;}=new(0,[]);[TempData]public string? Message{get;set;}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){var r=await RedirectIfProfileIncompleteAsync(true);if(r is not null)return r;return Page();}
 public async Task<IActionResult> OnPostOpenAsync(Guid id,CancellationToken ct){var x=await service.OpenAsync(UserId(),"BRAND",id,ct);if(x.Success&&x.TargetUrl is not null)return LocalRedirect(x.TargetUrl);Message=x.Message;return Back();}
 public async Task<IActionResult> OnPostReadAsync(Guid id,CancellationToken ct){Message=(await service.MarkReadAsync(UserId(),"BRAND",id,ct)).Message;return Back();}public async Task<IActionResult> OnPostAllAsync(CancellationToken ct){Message=(await service.MarkAllReadAsync(UserId(),"BRAND",ct)).Message;return Back();}public async Task<IActionResult> OnPostArchiveAsync(Guid id,CancellationToken ct){Message=(await service.ArchiveAsync(UserId(),"BRAND",id,ct)).Message;return Back();}public async Task<IActionResult> OnPostRestoreAsync(Guid id,CancellationToken ct){Message=(await service.RestoreAsync(UserId(),"BRAND",id,ct)).Message;return Back();}
 public async Task<IActionResult> OnGetDataAsync(int draw, int start, int length, string? query, string? filter, CancellationToken ct)
 {
  if (await RedirectIfProfileIncompleteAsync(true) is not null) return StatusCode(403);
  var result = await service.GetPageAsync(UserId(), "BRAND", filter, query, start, length, ct);
  return new JsonResult(new { draw = Math.Max(0, draw), recordsTotal = result.TotalCount, recordsFiltered = result.FilteredCount, data = result.Items, summary = new { result.UnreadCount, result.ActiveCount, result.ArchivedCount } });
 }
 public async Task<IActionResult> OnPostActionAsync(string action, Guid id, CancellationToken ct)
 {
  if (await RedirectIfProfileIncompleteAsync(true) is not null) return StatusCode(403);
  if (action == "open") { var opened = await service.OpenAsync(UserId(), "BRAND", id, ct); return new JsonResult(new { success = opened.Success, message = opened.Message, targetUrl = opened.TargetUrl }); }
  var result = action switch { "read" => await service.MarkReadAsync(UserId(), "BRAND", id, ct), "all" => await service.MarkAllReadAsync(UserId(), "BRAND", ct), "archive" => await service.ArchiveAsync(UserId(), "BRAND", id, ct), "restore" => await service.RestoreAsync(UserId(), "BRAND", id, ct), _ => new NotificationResult(false, "Thao tác không hợp lệ.") };
  return new JsonResult(new { success = result.Success, message = result.Message });
 }
 private RedirectToPageResult Back()=>RedirectToPage(new{filter=Filter});private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
