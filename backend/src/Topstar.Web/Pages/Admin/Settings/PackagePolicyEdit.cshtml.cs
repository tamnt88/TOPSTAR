using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Matching;
using Topstar.Web.Services.Storage;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class PackagePolicyEditModel(IPackagePolicyAdminService service,IFileStorageService storage):PageModel
{
 [BindProperty]public PackagePolicyForm Input{get;set;}=new();
 [BindProperty]public IFormFile? ImageFile{get;set;}
 public IReadOnlyList<PackagePolicyHistoryItem> History{get;private set;}=[];
 public async Task<JsonResult> OnPostEditorImageAsync(IFormFile? upload,CancellationToken ct)
 {
  var url=await storage.SavePackageImageAsync(upload,ct);
  if(url is null){Response.StatusCode=400;return new JsonResult(new{error=new{message="Ảnh phải là JPG, PNG hoặc WEBP và không vượt quá 5 MB."}});}
  return new JsonResult(new{url});
 }
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken ct)
 {
  var item=await service.GetByIdAsync(id,ct);if(item is null)return NotFound();
  Input=new(){Code=item.Code,Name=item.Name,ReferenceBudget=item.ReferenceBudget,SalePrice=item.SalePrice,ImageUrl=item.ImageUrl,Description=item.Description,Benefits=item.Benefits,ValidityDays=item.ValidityDays,IsFeatured=item.IsFeatured,DisplayOrder=item.DisplayOrder,MinBudgetAmount=item.MinBudgetAmount,MaxBudgetAmount=item.MaxBudgetAmount,BrandSelectionEnabled=item.BrandSelectionEnabled,SelectionQuota=item.SelectionQuota,IsActive=item.IsActive};History=item.History;return Page();
 }
 public async Task<IActionResult> OnPostAsync(Guid id,CancellationToken ct)
 {
  var existing=await service.GetByIdAsync(id,ct);if(existing is null)return NotFound();
  Input.Code=existing.Code;ModelState.Remove("Input.Code");
  Input.SalePrice=existing.SalePrice;Input.ValidityDays=existing.ValidityDays;
  Input.MinBudgetAmount=existing.MinBudgetAmount;Input.MaxBudgetAmount=existing.MaxBudgetAmount;
  Input.Reason="Cập nhật gói chiến dịch.";
  foreach(var field in new[]{"SalePrice","ValidityDays","MinBudgetAmount","MaxBudgetAmount","Reason"})ModelState.Remove("Input."+field);
  if(Input.ReferenceBudget is not >0)ModelState.AddModelError("Input.ReferenceBudget","Vui lòng nhập ngân sách trả KOC.");
  if(!ModelState.IsValid){await LoadHistory(id,ct);return Page();}
  if(ImageFile is not null)
  {
   var imageUrl=await storage.SavePackageImageAsync(ImageFile,ct);
   if(imageUrl is null){ModelState.AddModelError(nameof(ImageFile),"Ảnh phải là JPG, PNG hoặc WEBP hợp lệ và không vượt quá 5 MB.");await LoadHistory(id,ct);return Page();}
   Input.ImageUrl=imageUrl;
  }
  var admin=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  var result=await service.UpdateAsync(admin,new(id,Input.Name,Input.SalePrice,Input.ImageUrl,Input.Description,Input.Benefits,Input.ValidityDays,Input.IsFeatured,Input.DisplayOrder,Input.MinBudgetAmount,Input.MaxBudgetAmount,Input.BrandSelectionEnabled,Input.SelectionQuota,Input.IsActive,Input.Reason){ReferenceBudget=Input.ReferenceBudget},ct);
  if(!result.Success)
  {
   var errorKey=result.Message.Contains("ngân sách",StringComparison.OrdinalIgnoreCase)
    || result.Message.Contains("policy",StringComparison.OrdinalIgnoreCase)
    ? "Input.ReferenceBudget" : string.Empty;
   ModelState.AddModelError(errorKey,result.Message);await LoadHistory(id,ct);return Page();
  }
  TempData["Message"]=result.Message;return RedirectToPage("./PackagePolicies");
 }
 private async Task LoadHistory(Guid id,CancellationToken ct){History=(await service.GetByIdAsync(id,ct))?.History??[];}
}
