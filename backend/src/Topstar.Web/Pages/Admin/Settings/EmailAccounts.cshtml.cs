using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;
using Topstar.Web.Services.Email;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class EmailAccountsModel(ISystemSettingsAdminService service,IEmailAccountDiagnostics diagnostics):PageModel
{
 public IReadOnlyList<EmailAccountItem> Rows{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct)=>Rows=await service.GetEmailAccountsAsync(ct);
 public async Task<IActionResult> OnPostTestServerAsync(Guid id,CancellationToken ct){var result=await diagnostics.TestServerAsync(id,ct);TempData[result.Success?"AdminMessageSuccess":"AdminMessageError"]=result.Message;return RedirectToPage();}
 public async Task<IActionResult> OnPostSendTestAsync(Guid id,CancellationToken ct){var result=await diagnostics.SendTestAsync(id,ct);TempData[result.Success?"AdminMessageSuccess":"AdminMessageError"]=result.Message;return RedirectToPage();}
}
