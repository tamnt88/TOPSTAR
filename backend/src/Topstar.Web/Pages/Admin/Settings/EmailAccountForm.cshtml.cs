using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Topstar.Application.SystemSettings;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class EmailAccountFormModel(ISystemSettingsAdminService service):PageModel
{
 [BindProperty]public EmailAccountInput Input{get;set;}=new();public bool Editing=>Input.Id.HasValue;
 public async Task<IActionResult> OnGetAsync(Guid? id,CancellationToken ct){if(!id.HasValue)return Page();var x=await service.GetEmailAccountByIdAsync(id.Value,ct);if(x is null)return NotFound();Input=new(){Id=x.Id,Name=x.Name,EmailAddress=x.EmailAddress,DisplayName=x.DisplayName,SmtpHost=x.SmtpHost,SmtpPort=x.SmtpPort,SmtpUsername=x.SmtpUsername,SmtpEnableSsl=x.SmtpEnableSsl,ImapHost=x.ImapHost,ImapPort=x.ImapPort,ImapUsername=x.ImapUsername,ImapEnableSsl=x.ImapEnableSsl,IsDefaultSender=x.IsDefaultSender,IsDefaultReceiver=x.IsDefaultReceiver,IsActive=x.IsActive,IsVisible=true};return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var request=new EmailAccountUpsertRequest(Input.Id,Input.Name,Input.EmailAddress,Input.DisplayName,Input.SmtpHost,Input.SmtpPort,Input.SmtpUsername,Input.SmtpPassword,Input.SmtpEnableSsl,Input.ImapHost,Input.ImapPort,Input.ImapUsername,Input.ImapPassword,Input.ImapEnableSsl,Input.IsDefaultSender,Input.IsDefaultReceiver,Input.IsActive,Input.IsVisible);var r=Input.Id.HasValue?await service.UpdateEmailAccountAsync(request,ct):await service.CreateEmailAccountAsync(request,ct);if(!r.Success){ModelState.AddModelError(string.Empty,r.Message);return Page();}TempData["AdminMessageSuccess"]=r.Message;return RedirectToPage("EmailAccounts");}
}
