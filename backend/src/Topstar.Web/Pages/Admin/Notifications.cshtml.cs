using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Notifications;
namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class NotificationsModel(INotificationInboxService service):PageModel
{
 [BindProperty(SupportsGet=true)]public string Filter{get;set;}="all";public NotificationPage PageData{get;private set;}=new(0,0,0,0,0,[]);[TempData]public string? Message{get;set;}
 public async Task OnGetAsync(CancellationToken ct)=>PageData=await service.GetPageAsync(UserId(),"ADMIN",Filter,null,0,100,ct);
 public async Task<IActionResult> OnPostOpenAsync(Guid id,CancellationToken ct){var x=await service.OpenAsync(UserId(),"ADMIN",id,ct);if(x.Success&&x.TargetUrl is not null)return LocalRedirect(x.TargetUrl);Message=x.Message;return Back();}
 public async Task<IActionResult> OnPostReadAsync(Guid id,CancellationToken ct){Message=(await service.MarkReadAsync(UserId(),"ADMIN",id,ct)).Message;return Back();}
 public async Task<IActionResult> OnPostAllAsync(CancellationToken ct){Message=(await service.MarkAllReadAsync(UserId(),"ADMIN",ct)).Message;return Back();}
 public async Task<IActionResult> OnPostArchiveAsync(Guid id,CancellationToken ct){Message=(await service.ArchiveAsync(UserId(),"ADMIN",id,ct)).Message;return Back();}
 public async Task<IActionResult> OnPostRestoreAsync(Guid id,CancellationToken ct){Message=(await service.RestoreAsync(UserId(),"ADMIN",id,ct)).Message;return Back();}
 private RedirectToPageResult Back()=>RedirectToPage(new{filter=Filter});private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
