using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Topstar.Application.Menus;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class MenuDeleteModel(IMenuAdminService service):PageModel
{
 public Guid Id{get;private set;} public string Title{get;private set;}="";public string? Path{get;private set;}
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken token){var item=await service.GetByIdAsync(id,token);if(item is null)return NotFound();Id=item.Id;Title=item.Title;Path=item.Path;return Page();}
 public async Task<IActionResult> OnPostAsync(Guid id,CancellationToken token){var result=await service.DeleteAsync(id,token);TempData[result.Success?"AdminMessageSuccess":"AdminMessageError"]=result.Message;return RedirectToPage("MenuManagement");}
}
