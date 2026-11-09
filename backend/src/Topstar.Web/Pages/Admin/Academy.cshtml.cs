using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Academy;
using Topstar.Web.Services.Storage;
namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class AcademyModel(IAcademyService service, IAcademyLandingService landingService, IFileStorageService fileStorage):PageModel
{
 public IReadOnlyList<AcademyCourseItem> Items{get;private set;}=[];
 public IReadOnlyList<AcademyAdminEnrollmentItem> Enrollments{get;private set;}=[];
 public IReadOnlyList<AcademyLandingVersionItem> LandingHistory{get;private set;}=[];
 [BindProperty] public LandingInput Landing{get;set;}=new();
 [TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
 public async Task OnGetAsync(CancellationToken ct){await LoadAsync(ct);Landing=LandingInput.From(await landingService.GetAsync(ct));}
 public async Task<IActionResult> OnPostCreateAsync(string title,string level,int durationWeeks,string summary,string? syllabus,int orderNo,CancellationToken ct)=>Done(await service.CreateAsync(UserId(),title,level,durationWeeks,summary,syllabus,orderNo,ct));
 public async Task<IActionResult> OnPostUpdateAsync(Guid id,string title,string level,int durationWeeks,string summary,string? syllabus,int orderNo,CancellationToken ct)=>Done(await service.UpdateAsync(UserId(),id,title,level,durationWeeks,summary,syllabus,orderNo,"Cập nhật thông tin khóa học.",ct));
 public async Task<IActionResult> OnPostStatusAsync(Guid id,string status,CancellationToken ct)=>Done(await service.ChangeStatusAsync(UserId(),id,status,"Cập nhật trạng thái khóa học.",ct));
 public async Task<IActionResult> OnPostLessonAsync(Guid courseId,Guid? lessonId,string title,string? contentUrl,int orderNo,bool isActive,CancellationToken ct)=>Done(await service.SaveLessonAsync(UserId(),courseId,lessonId,title,contentUrl,orderNo,isActive,"Cập nhật bài học Học viện KOC.",ct));
 public async Task<IActionResult> OnPostLandingAsync(CancellationToken ct)
 {
  ValidateLanding();
  if(!ModelState.IsValid){await LoadAsync(ct);return Page();}
  var result=await landingService.SaveAsync(Landing.ToContent(),"Cập nhật nội dung Học viện KOC.",UserId(),ct);
  if(!result.Success){ModelState.AddModelError(string.Empty,result.Message);await LoadAsync(ct);return Page();}
  Message=result.Message;MessageSuccess=true;return RedirectToPage(null,null,"tab-content");
 }
 public async Task<JsonResult> OnPostUploadImageAsync(IFormFile? imageFile,CancellationToken ct)
 {
  var url=await fileStorage.SaveImageAsync(imageFile,"uploads/academy",ct);
  return url is null?new JsonResult(new{success=false,message="Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB."}){StatusCode=400}:new JsonResult(new{success=true,url});
 }
 private async Task LoadAsync(CancellationToken ct){Items=await service.GetAdminAsync(ct);Enrollments=await service.GetAdminEnrollmentsAsync(ct);LandingHistory=await landingService.GetHistoryAsync(ct);}
 private void ValidateLanding()
 {
  Check(Landing.HeroTitle,10,180,"Landing.HeroTitle","Vui lòng nhập tiêu đề banner từ 10 đến 180 ký tự.");
  Check(Landing.HeroDescription,20,600,"Landing.HeroDescription","Vui lòng nhập mô tả banner từ 20 đến 600 ký tự.");
  Check(Landing.HeroImageUrl,1,500,"Landing.HeroImageUrl","Vui lòng tải ảnh banner.");
  ValidateCount(Landing.Roadmap.Count,1,8,"Lộ trình");ValidateCount(Landing.TrustItems.Count,1,8,"Chuẩn đầu ra");ValidateCount(Landing.QualityItems.Count,1,6,"Quy tắc chất lượng");ValidateCount(Landing.Outcomes.Count,1,8,"Năng lực sau lộ trình");ValidateCount(Landing.Resources.Count,1,8,"Tài nguyên");
 }
 private void ValidateCount(int count,int min,int max,string label){if(count<min||count>max)ModelState.AddModelError(string.Empty,$"{label} phải có từ {min} đến {max} mục.");}
 private void Check(string? value,int min,int max,string key,string message){var n=value?.Trim().Length??0;if(n<min||n>max)ModelState.AddModelError(key,message);}
 private IActionResult Done(AcademyResult r){Message=r.Message;MessageSuccess=r.Success;return RedirectToPage();}
 private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

 public sealed class LandingInput
 {
  public string Eyebrow{get;set;}="";public string HeroTitle{get;set;}="";public string HeroHighlight{get;set;}="";public string HeroDescription{get;set;}="";public string HeroImageUrl{get;set;}="";
  public string PrimaryCtaText{get;set;}="";public string PrimaryCtaUrl{get;set;}="";public string SecondaryCtaText{get;set;}="";public string SecondaryCtaUrl{get;set;}="";
  public string RoadmapTitle{get;set;}="";public string RoadmapDescription{get;set;}="";public string TrustTitle{get;set;}="";public string TrustDescription{get;set;}="";public string QualityTitle{get;set;}="";
  public string OutcomeTitle{get;set;}="";public string OutcomeDescription{get;set;}="";public string ResourceTitle{get;set;}="";public string ResourceDescription{get;set;}="";
  public string FinalCtaTitle{get;set;}="";public string FinalCtaDescription{get;set;}="";public string FinalCtaText{get;set;}="";public string FinalCtaUrl{get;set;}="";public bool IsPublished{get;set;}
  public List<StatInput> Stats{get;set;}=[];public List<RoadmapInput> Roadmap{get;set;}=[];public List<CardInput> TrustItems{get;set;}=[];public List<CardInput> QualityItems{get;set;}=[];public List<CardInput> Outcomes{get;set;}=[];public List<ResourceInput> Resources{get;set;}=[];
  public static LandingInput From(AcademyLandingContent x)=>new(){Eyebrow=x.Eyebrow,HeroTitle=x.HeroTitle,HeroHighlight=x.HeroHighlight,HeroDescription=x.HeroDescription,HeroImageUrl=x.HeroImageUrl,PrimaryCtaText=x.PrimaryCtaText,PrimaryCtaUrl=x.PrimaryCtaUrl,SecondaryCtaText=x.SecondaryCtaText,SecondaryCtaUrl=x.SecondaryCtaUrl,RoadmapTitle=x.RoadmapTitle,RoadmapDescription=x.RoadmapDescription,TrustTitle=x.TrustTitle,TrustDescription=x.TrustDescription,QualityTitle=x.QualityTitle,OutcomeTitle=x.OutcomeTitle,OutcomeDescription=x.OutcomeDescription,ResourceTitle=x.ResourceTitle,ResourceDescription=x.ResourceDescription,FinalCtaTitle=x.FinalCtaTitle,FinalCtaDescription=x.FinalCtaDescription,FinalCtaText=x.FinalCtaText,FinalCtaUrl=x.FinalCtaUrl,IsPublished=x.IsPublished,Stats=x.Stats.Select(StatInput.From).ToList(),Roadmap=x.Roadmap.Select(RoadmapInput.From).ToList(),TrustItems=x.TrustItems.Select(CardInput.From).ToList(),QualityItems=x.QualityItems.Select(CardInput.From).ToList(),Outcomes=x.Outcomes.Select(CardInput.From).ToList(),Resources=x.Resources.Select(ResourceInput.From).ToList()};
  public AcademyLandingContent ToContent()=>new(Eyebrow,HeroTitle,HeroHighlight,HeroDescription,HeroImageUrl,PrimaryCtaText,PrimaryCtaUrl,SecondaryCtaText,SecondaryCtaUrl,RoadmapTitle,RoadmapDescription,TrustTitle,TrustDescription,QualityTitle,OutcomeTitle,OutcomeDescription,ResourceTitle,ResourceDescription,FinalCtaTitle,FinalCtaDescription,FinalCtaText,FinalCtaUrl,IsPublished,Stats.Select(x=>x.ToItem()).ToArray(),Roadmap.Select(x=>x.ToItem()).ToArray(),TrustItems.Select(x=>x.ToItem()).ToArray(),QualityItems.Select(x=>x.ToItem()).ToArray(),Outcomes.Select(x=>x.ToItem()).ToArray(),Resources.Select(x=>x.ToItem()).ToArray());
 }
 public sealed class StatInput{public string Value{get;set;}="";public string Label{get;set;}="";public int DisplayOrder{get;set;}public bool IsVisible{get;set;}public AcademyLandingStat ToItem()=>new(Value,Label,DisplayOrder,IsVisible);public static StatInput From(AcademyLandingStat x)=>new(){Value=x.Value,Label=x.Label,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible};}
 public sealed class RoadmapInput{public string Stage{get;set;}="";public string Title{get;set;}="";public string Description{get;set;}="";public int DisplayOrder{get;set;}public bool IsVisible{get;set;}public AcademyLandingRoadmap ToItem()=>new(Stage,Title,Description,DisplayOrder,IsVisible);public static RoadmapInput From(AcademyLandingRoadmap x)=>new(){Stage=x.Stage,Title=x.Title,Description=x.Description,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible};}
 public sealed class CardInput{public string Icon{get;set;}="";public string Title{get;set;}="";public string Description{get;set;}="";public int DisplayOrder{get;set;}public bool IsVisible{get;set;}public AcademyLandingCard ToItem()=>new(Icon,Title,Description,DisplayOrder,IsVisible);public static CardInput From(AcademyLandingCard x)=>new(){Icon=x.Icon,Title=x.Title,Description=x.Description,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible};}
 public sealed class ResourceInput{public string Icon{get;set;}="";public string Title{get;set;}="";public int DisplayOrder{get;set;}public bool IsVisible{get;set;}public AcademyLandingResource ToItem()=>new(Icon,Title,DisplayOrder,IsVisible);public static ResourceInput From(AcademyLandingResource x)=>new(){Icon=x.Icon,Title=x.Title,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible};}
}
