using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;
using Topstar.Web.Services.Storage;
namespace Topstar.Web.Pages.Admin.Settings;
[Authorize(Roles="Admin")]
public sealed class SeoSocialFormModel(ISystemSettingsAdminService service,IFileStorageService storage):PageModel
{
 [BindProperty] public InputModel Input{get;set;}=new();
 [BindProperty] public IFormFile? OgImageFile{get;set;}
 [BindProperty] public IFormFile? TwitterImageFile{get;set;}
 public bool Editing=>Input.Id.HasValue;
 public async Task<JsonResult> OnPostEditorImageAsync(IFormFile? upload, CancellationToken token)
 {
  var url = await storage.SaveCampaignImageAsync(upload, token);
  return url is null
   ? new JsonResult(new { error = new { message = "Ảnh phải là JPG, PNG hoặc WEBP, tối đa 5 MB." } }) { StatusCode = 400 }
   : new JsonResult(new { url });
 }
 public async Task<IActionResult> OnGetAsync(Guid? id,CancellationToken token){if(!id.HasValue)return Page();var x=await service.GetSeoConfigByIdAsync(id.Value,token);if(x is null)return NotFound();Input=new(){Id=x.Id,PageTitle=x.PageTitle,ShortDescription=x.ShortDescription,PageContent=x.PageContent,ViewCount=x.ViewCount,PageKey=x.PageKey,PagePath=x.PagePath,SeoTitle=x.SeoTitle,SeoDescription=x.SeoDescription,SeoKeywords=x.SeoKeywords,CanonicalUrl=x.CanonicalUrl,OgTitle=x.OgTitle,OgDescription=x.OgDescription,OgImageUrl=x.OgImageUrl,TwitterTitle=x.TwitterTitle,TwitterDescription=x.TwitterDescription,TwitterImageUrl=x.TwitterImageUrl,JsonLd=x.JsonLd,IsActive=x.IsActive,IsVisible=x.IsVisible};return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken token){if(!ModelState.IsValid)return Page();var og=await storage.SaveImageAsync(OgImageFile,"uploads/seo",token)??Input.OgImageUrl;var tw=await storage.SaveImageAsync(TwitterImageFile,"uploads/seo",token)??Input.TwitterImageUrl;var request=new SeoConfigUpsertRequest(Input.Id,Input.PageKey,Input.PagePath,Input.SeoTitle,Input.SeoDescription,Input.SeoKeywords,Input.CanonicalUrl,Input.OgTitle,Input.OgDescription,og,Input.TwitterTitle,Input.TwitterDescription,tw,Input.JsonLd,Input.IsActive,Input.IsVisible){PageTitle=Input.PageTitle,ShortDescription=Input.ShortDescription,PageContent=Input.PageContent};var result=Editing?await service.UpdateSeoConfigAsync(request,token):await service.CreateSeoConfigAsync(request,token);if(!result.Success){ModelState.AddModelError(string.Empty,result.Message);return Page();}TempData["AdminMessageSuccess"]=result.Message;return RedirectToPage("SeoSocial");}
 public sealed class InputModel
 {
  public Guid? Id{get;set;}
  [StringLength(200)] public string? PageTitle { get; set; }
  [StringLength(1000)] public string? ShortDescription { get; set; }
  [StringLength(100000)] public string? PageContent { get; set; }
  public long ViewCount { get; set; }
  [Required(ErrorMessage="Vui lòng nhập mã trang.")][StringLength(80,MinimumLength=2,ErrorMessage="Mã trang phải từ 2 đến 80 ký tự.")] public string PageKey{get;set;}="";
  [Required(ErrorMessage="Vui lòng nhập đường dẫn trang.")][StringLength(250,ErrorMessage="Đường dẫn không được vượt quá 250 ký tự.")][RegularExpression("^/.*",ErrorMessage="Đường dẫn phải bắt đầu bằng dấu /. ")] public string PagePath{get;set;}="";
  [StringLength(200)] public string? SeoTitle{get;set;} [StringLength(500)] public string? SeoDescription{get;set;} [StringLength(500)] public string? SeoKeywords{get;set;} [StringLength(500)] public string? CanonicalUrl{get;set;}
  [StringLength(200)] public string? OgTitle{get;set;} [StringLength(500)] public string? OgDescription{get;set;} public string? OgImageUrl{get;set;}
  [StringLength(200)] public string? TwitterTitle{get;set;} [StringLength(500)] public string? TwitterDescription{get;set;} public string? TwitterImageUrl{get;set;} public string? JsonLd{get;set;}
  public bool IsActive{get;set;}=true;public bool IsVisible{get;set;}=true;
 }
}
