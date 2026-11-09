using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;
using Topstar.Web.Services.Storage;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class PackagePolicyCreateModel(IPackagePolicyAdminService service,IFileStorageService storage):PageModel
{
 [BindProperty]public PackagePolicyForm Input{get;set;}=new(){IsActive=false};
 [BindProperty]public IFormFile? ImageFile{get;set;}
 public void OnGet(){}
 public async Task<JsonResult> OnPostEditorImageAsync(IFormFile? upload,CancellationToken ct)
 {
  var url=await storage.SavePackageImageAsync(upload,ct);
  if(url is null){Response.StatusCode=400;return new JsonResult(new{error=new{message="Ảnh phải là JPG, PNG hoặc WEBP và không vượt quá 5 MB."}});}
  return new JsonResult(new{url});
 }
 public async Task<IActionResult> OnPostAsync(CancellationToken ct)
 {
  Input.Code="PKG_"+Guid.NewGuid().ToString("N").ToUpperInvariant();ModelState.Remove("Input.Code");
  Input.Reason="Tạo gói chiến dịch.";Input.ValidityDays=null;Input.MinBudgetAmount=0;Input.MaxBudgetAmount=null;
  ModelState.Remove("Input.Reason");ModelState.Remove("Input.ValidityDays");ModelState.Remove("Input.MinBudgetAmount");ModelState.Remove("Input.MaxBudgetAmount");
  if(Input.ReferenceBudget is not >0)ModelState.AddModelError("Input.ReferenceBudget","Vui lòng nhập ngân sách tham khảo.");
  if(Input.SalePrice<=0&&Input.ReferenceBudget>0){Input.SalePrice=Input.ReferenceBudget.Value;ModelState.Remove("Input.SalePrice");}
  if(!ModelState.IsValid)return Page();
  if(ImageFile is not null)
  {
   Input.ImageUrl=await storage.SavePackageImageAsync(ImageFile,ct);
   if(Input.ImageUrl is null){ModelState.AddModelError(nameof(ImageFile),"Ảnh phải là JPG, PNG hoặc WEBP hợp lệ và không vượt quá 5 MB.");return Page();}
  }
  var admin=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  var result=await service.CreateAsync(admin,new(Input.Code,Input.Name,Input.SalePrice,Input.ImageUrl,Input.Description,Input.Benefits,Input.ValidityDays,Input.IsFeatured,Input.DisplayOrder,Input.MinBudgetAmount,Input.MaxBudgetAmount,Input.BrandSelectionEnabled,Input.SelectionQuota,Input.IsActive,Input.Reason){ReferenceBudget=Input.ReferenceBudget},ct);
  if(!result.Success)
  {
   var errorKey=result.Message.Contains("ngân sách",StringComparison.OrdinalIgnoreCase)
    || result.Message.Contains("policy",StringComparison.OrdinalIgnoreCase)
    ? "Input.ReferenceBudget" : string.Empty;
   ModelState.AddModelError(errorKey,result.Message);return Page();
  }
  TempData["Message"]=result.Message;return RedirectToPage("./PackagePolicies");
 }
}
