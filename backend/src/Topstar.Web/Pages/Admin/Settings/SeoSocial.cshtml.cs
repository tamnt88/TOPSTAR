using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class SeoSocialModel(
    ISystemSettingsAdminService service,
    IFileStorageService fileStorageService) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? EditId { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? OgImageFile { get; set; }
    [BindProperty] public IFormFile? TwitterImageFile { get; set; }

    public IReadOnlyList<SeoConfigItem> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Rows = await service.GetSeoConfigsAsync(HttpContext.RequestAborted);
        if (!EditId.HasValue || EditId.Value == Guid.Empty) return;

        var item = await service.GetSeoConfigByIdAsync(EditId.Value, HttpContext.RequestAborted);
        if (item is null) return;

        Input = new InputModel
        {
            Id = item.Id,
            PageKey = item.PageKey,
            PagePath = item.PagePath,
            SeoTitle = item.SeoTitle,
            SeoDescription = item.SeoDescription,
            SeoKeywords = item.SeoKeywords,
            CanonicalUrl = item.CanonicalUrl,
            OgTitle = item.OgTitle,
            OgDescription = item.OgDescription,
            OgImageUrl = item.OgImageUrl,
            TwitterTitle = item.TwitterTitle,
            TwitterDescription = item.TwitterDescription,
            TwitterImageUrl = item.TwitterImageUrl,
            JsonLd = item.JsonLd,
            IsActive = item.IsActive,
            IsVisible = item.IsVisible
        };
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var ogImageUrl = await fileStorageService.SaveImageAsync(OgImageFile, "uploads/seo", HttpContext.RequestAborted) ?? Input.OgImageUrl;
        var twitterImageUrl = await fileStorageService.SaveImageAsync(TwitterImageFile, "uploads/seo", HttpContext.RequestAborted) ?? Input.TwitterImageUrl;

        var request = new SeoConfigUpsertRequest(
            Input.Id,
            Input.PageKey,
            Input.PagePath,
            Input.SeoTitle,
            Input.SeoDescription,
            Input.SeoKeywords,
            Input.CanonicalUrl,
            Input.OgTitle,
            Input.OgDescription,
            ogImageUrl,
            Input.TwitterTitle,
            Input.TwitterDescription,
            twitterImageUrl,
            Input.JsonLd,
            Input.IsActive,
            Input.IsVisible);

        var rs = Input.Id.HasValue
            ? await service.UpdateSeoConfigAsync(request, HttpContext.RequestAborted)
            : await service.CreateSeoConfigAsync(request, HttpContext.RequestAborted);

        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/SeoSocial");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var rs = await service.DeleteSeoConfigAsync(id, HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/SeoSocial");
    }

    public sealed class InputModel
    {
        public Guid? Id { get; set; }
        public string PageKey { get; set; } = string.Empty;
        public string PagePath { get; set; } = string.Empty;
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImageUrl { get; set; }
        public string? TwitterTitle { get; set; }
        public string? TwitterDescription { get; set; }
        public string? TwitterImageUrl { get; set; }
        public string? JsonLd { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
