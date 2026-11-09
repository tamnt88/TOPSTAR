using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Notifications;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class NotificationTemplatesModel(INotificationTemplateAdminService service):PageModel
{
 public IReadOnlyList<NotificationTemplateItem> Items{get;private set;}=[];
 [TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 public async Task OnGetAsync(CancellationToken ct)=>Items=await service.GetAllAsync(ct);
 public async Task<IActionResult> OnPostSaveAsync(Guid? id,string type,string audienceRole,string titleTemplate,string bodyTemplate,bool isActive,CancellationToken ct)
 {var actor=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var result=await service.SaveAsync(actor,id,type,audienceRole,titleTemplate,bodyTemplate,isActive,"Cập nhật nội dung mẫu thông báo.",ct);Message=result.Message;Success=result.Success;return RedirectToPage();}
}
