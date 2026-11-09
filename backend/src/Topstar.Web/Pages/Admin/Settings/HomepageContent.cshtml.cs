using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Website;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class HomepageContentModel(IHomepageContentService service, IFileStorageService fileStorage) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public IReadOnlyList<HomepageVersionItem> History { get; private set; } = Array.Empty<HomepageVersionItem>();

    public async Task OnGetAsync()
    {
        Input = InputModel.From(await service.GetAsync(HttpContext.RequestAborted));
        History = await service.GetHistoryAsync(HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        ValidateCollections();
        if (!ModelState.IsValid)
        {
            History = await service.GetHistoryAsync(HttpContext.RequestAborted);
            return Page();
        }

        Guid? actor = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;
        var result = await service.SaveAsync(Input.ToContent(), "Cập nhật nội dung trang chủ.", actor, HttpContext.RequestAborted);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            History = await service.GetHistoryAsync(HttpContext.RequestAborted);
            return Page();
        }
        TempData["AdminMessageSuccess"] = result.Message;
        return RedirectToPage();
    }

    private void ValidateCollections()
    {
        ValidateCount(Input.Features.Count, 1, 8, "Lợi ích nổi bật");
        ValidateCount(Input.ProcessSteps.Count, 1, 8, "Các bước triển khai");
        ValidateCount(Input.Creators.Count, 1, 12, "KOC/KOL nổi bật");
        ValidateCount(Input.Partners.Count, 1, 24, "Đối tác");

        for (var i = 0; i < Input.Features.Count; i++)
        {
            Require(Input.Features[i].Title, $"Input.Features[{i}].Title", "Vui lòng nhập tiêu đề lợi ích.");
            Require(Input.Features[i].Description, $"Input.Features[{i}].Description", "Vui lòng nhập mô tả lợi ích.");
            Require(Input.Features[i].IconClass, $"Input.Features[{i}].IconClass", "Vui lòng tải icon lợi ích.");
        }
        for (var i = 0; i < Input.ProcessSteps.Count; i++)
        {
            Require(Input.ProcessSteps[i].StepNumber, $"Input.ProcessSteps[{i}].StepNumber", "Vui lòng nhập số thứ tự bước.");
            Require(Input.ProcessSteps[i].Title, $"Input.ProcessSteps[{i}].Title", "Vui lòng nhập tiêu đề bước.");
            Require(Input.ProcessSteps[i].Description, $"Input.ProcessSteps[{i}].Description", "Vui lòng nhập mô tả bước.");
        }
        for (var i = 0; i < Input.Creators.Count; i++)
        {
            Require(Input.Creators[i].Name, $"Input.Creators[{i}].Name", "Vui lòng nhập tên KOC/KOL.");
            Require(Input.Creators[i].Category, $"Input.Creators[{i}].Category", "Vui lòng nhập nhóm nội dung.");
            Require(Input.Creators[i].Followers, $"Input.Creators[{i}].Followers", "Vui lòng nhập số người theo dõi.");
            Require(Input.Creators[i].ImageUrl, $"Input.Creators[{i}].ImageUrl", "Vui lòng tải ảnh KOC/KOL.");
        }
        for (var i = 0; i < Input.Partners.Count; i++)
        {
            Require(Input.Partners[i].Name, $"Input.Partners[{i}].Name", "Vui lòng nhập tên đối tác.");
            Require(Input.Partners[i].LogoUrl, $"Input.Partners[{i}].LogoUrl", "Vui lòng tải logo đối tác.");
        }
    }

    private void ValidateCount(int count, int min, int max, string label)
    {
        if (count < min || count > max)
            ModelState.AddModelError(string.Empty, $"{label} phải có từ {min} đến {max} mục.");
    }

    private void Require(string? value, string key, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) ModelState.AddModelError(key, message);
    }

    public async Task<JsonResult> OnPostUploadImageAsync(IFormFile? imageFile)
    {
        var url = await fileStorage.SaveImageAsync(imageFile, "uploads/homepage", HttpContext.RequestAborted);
        return url is null
            ? new JsonResult(new { success = false, message = "Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB." }) { StatusCode = 400 }
            : new JsonResult(new { success = true, url });
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập nhãn tin cậy.")] [StringLength(120)] public string TrustLabel { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner.")] [StringLength(180, MinimumLength = 10)] public string HeroTitle { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập mô tả banner.")] [StringLength(600, MinimumLength = 20)] public string HeroDescription { get; set; } = "";
        [Required] [StringLength(80)] public string PrimaryCtaText { get; set; } = "";
        [Required] [StringLength(300)] public string PrimaryCtaUrl { get; set; } = "";
        [Required] [StringLength(80)] public string SecondaryCtaText { get; set; } = "";
        [Required] [StringLength(300)] public string SecondaryCtaUrl { get; set; } = "";
        [Required] [StringLength(500)] public string HeroImageUrl { get; set; } = "";
        [Required] [StringLength(120)] public string AboutEyebrow { get; set; } = "";
        [Required] [StringLength(180)] public string AboutTitle { get; set; } = "";
        [Required] [StringLength(800)] public string AboutDescription { get; set; } = "";
        [Required] [StringLength(180)] public string ProcessTitle { get; set; } = "";
        [Required] [StringLength(180)] public string PartnerTitle { get; set; } = "";
        [Required] [StringLength(500)] public string PartnerDescription { get; set; } = "";
        [Required] [StringLength(180)] public string MetricsTitle { get; set; } = "";
        [Required] [StringLength(180)] public string FinalCtaTitle { get; set; } = "";
        [Required] [StringLength(500)] public string FinalCtaDescription { get; set; } = "";
        [Required] [StringLength(80)] public string FinalPrimaryCtaText { get; set; } = "";
        [Required] [StringLength(300)] public string FinalPrimaryCtaUrl { get; set; } = "";
        [Required] [StringLength(80)] public string FinalSecondaryCtaText { get; set; } = "";
        [Required] [StringLength(300)] public string FinalSecondaryCtaUrl { get; set; } = "";
        public bool ShowMetrics { get; set; }
        public bool ShowPartners { get; set; }
        public bool ShowFeaturedCreators { get; set; }
        public bool IsPublished { get; set; }
        public List<CreatorInput> Creators { get; set; } = [];
        public List<FeatureInput> Features { get; set; } = [];
        public List<ProcessInput> ProcessSteps { get; set; } = [];
        public List<MetricInput> Metrics { get; set; } = [];
        public List<PartnerInput> Partners { get; set; } = [];

        public static InputModel From(HomepageContent x) => new()
        {
            TrustLabel=x.TrustLabel,HeroTitle=x.HeroTitle,HeroDescription=x.HeroDescription,PrimaryCtaText=x.PrimaryCtaText,PrimaryCtaUrl=x.PrimaryCtaUrl,
            SecondaryCtaText=x.SecondaryCtaText,SecondaryCtaUrl=x.SecondaryCtaUrl,HeroImageUrl=x.HeroImageUrl,AboutEyebrow=x.AboutEyebrow,
            AboutTitle=x.AboutTitle,AboutDescription=x.AboutDescription,ProcessTitle=x.ProcessTitle,PartnerTitle=x.PartnerTitle,PartnerDescription=x.PartnerDescription,
            MetricsTitle=x.MetricsTitle,FinalCtaTitle=x.FinalCtaTitle,FinalCtaDescription=x.FinalCtaDescription,FinalPrimaryCtaText=x.FinalPrimaryCtaText,
            FinalPrimaryCtaUrl=x.FinalPrimaryCtaUrl,FinalSecondaryCtaText=x.FinalSecondaryCtaText,FinalSecondaryCtaUrl=x.FinalSecondaryCtaUrl,
            ShowMetrics=x.ShowMetrics,ShowPartners=x.ShowPartners,ShowFeaturedCreators=x.ShowFeaturedCreators,IsPublished=x.IsPublished,
            Creators=x.Creators.Select(CreatorInput.From).ToList(),Features=x.Features.Select(FeatureInput.From).ToList(),ProcessSteps=x.ProcessSteps.Select(ProcessInput.From).ToList(),
            Metrics=x.Metrics.Select(MetricInput.From).ToList(),Partners=x.Partners.Select(PartnerInput.From).ToList()
        };
        public HomepageContent ToContent() => new(TrustLabel,HeroTitle,HeroDescription,PrimaryCtaText,PrimaryCtaUrl,SecondaryCtaText,SecondaryCtaUrl,HeroImageUrl,
            AboutEyebrow,AboutTitle,AboutDescription,ProcessTitle,PartnerTitle,PartnerDescription,MetricsTitle,FinalCtaTitle,FinalCtaDescription,
            FinalPrimaryCtaText,FinalPrimaryCtaUrl,FinalSecondaryCtaText,FinalSecondaryCtaUrl,ShowMetrics,ShowPartners,ShowFeaturedCreators,IsPublished,
            Creators.Select(x=>x.ToItem()).ToArray(),Features.Select(x=>x.ToItem()).ToArray(),ProcessSteps.Select(x=>x.ToItem()).ToArray(),Metrics.Select(x=>x.ToItem()).ToArray(),Partners.Select(x=>x.ToItem()).ToArray());
    }
    public sealed class CreatorInput { public string Name{get;set;}=""; public string Category{get;set;}=""; public string Followers{get;set;}=""; public string ImageUrl{get;set;}=""; public int DisplayOrder{get;set;} public bool IsVisible{get;set;} public HomepageCreatorItem ToItem()=>new(Name,Category,Followers,ImageUrl,DisplayOrder,IsVisible); public static CreatorInput From(HomepageCreatorItem x)=>new(){Name=x.Name,Category=x.Category,Followers=x.Followers,ImageUrl=x.ImageUrl,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible}; }
    public sealed class FeatureInput { public string IconClass{get;set;}=""; public string Title{get;set;}=""; public string Description{get;set;}=""; public int DisplayOrder{get;set;} public bool IsVisible{get;set;} public HomepageFeatureItem ToItem()=>new(IconClass,Title,Description,DisplayOrder,IsVisible); public static FeatureInput From(HomepageFeatureItem x)=>new(){IconClass=x.IconClass,Title=x.Title,Description=x.Description,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible}; }
    public sealed class ProcessInput { public string StepNumber{get;set;}=""; public string Title{get;set;}=""; public string Description{get;set;}=""; public int DisplayOrder{get;set;} public bool IsVisible{get;set;} public HomepageProcessItem ToItem()=>new(StepNumber,Title,Description,DisplayOrder,IsVisible); public static ProcessInput From(HomepageProcessItem x)=>new(){StepNumber=x.StepNumber,Title=x.Title,Description=x.Description,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible}; }
    public sealed class MetricInput { public string Value{get;set;}=""; public string Label{get;set;}=""; public int DisplayOrder{get;set;} public bool IsVisible{get;set;} public HomepageMetricItem ToItem()=>new(Value,Label,DisplayOrder,IsVisible); public static MetricInput From(HomepageMetricItem x)=>new(){Value=x.Value,Label=x.Label,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible}; }
    public sealed class PartnerInput { public string Name{get;set;}=""; public string LogoUrl{get;set;}=""; public string Url{get;set;}="#"; public int DisplayOrder{get;set;} public bool IsVisible{get;set;} public HomepagePartnerItem ToItem()=>new(Name,LogoUrl,Url,DisplayOrder,IsVisible); public static PartnerInput From(HomepagePartnerItem x)=>new(){Name=x.Name,LogoUrl=x.LogoUrl,Url=x.Url,DisplayOrder=x.DisplayOrder,IsVisible=x.IsVisible}; }
}
