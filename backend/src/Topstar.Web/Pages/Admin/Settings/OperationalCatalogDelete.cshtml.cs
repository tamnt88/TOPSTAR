using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Settings;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class OperationalCatalogDeleteModel(IOperationalSetupService service):PageModel
{
 public OperationalCatalogItem Item{get;private set;}=null!;[BindProperty,Required,StringLength(500,MinimumLength=10)]public string Reason{get;set;}="";
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken ct){var item=await service.FindCatalogAsync(id,ct);if(item is null)return NotFound();Item=item;return Page();}
 public async Task<IActionResult> OnPostAsync(Guid id,CancellationToken ct){var item=await service.FindCatalogAsync(id,ct);if(item is null)return NotFound();Item=item;if(!ModelState.IsValid)return Page();var r=await service.DeleteCatalogAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),id,Reason,ct);if(!r.Success){ModelState.AddModelError(string.Empty,r.Message);return Page();}TempData["Message"]=r.Message;TempData["Success"]=true;return RedirectToPage("OperationalSetup");}
}
