using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class BrandingModel(
    ISystemSettingsAdminService service,
    IFileStorageService fileStorageService) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? LogoFile { get; set; }
    [BindProperty] public IFormFile? FaviconFile { get; set; }
    [BindProperty] public IFormFile? DefaultShareImageFile { get; set; }

    public async Task OnGetAsync()
    {
        var cfg = await service.GetWebsiteConfigAsync(HttpContext.RequestAborted);
        if (cfg is null) return;
        Input = new InputModel
        {
            LogoUrl = cfg.LogoUrl,
            FaviconUrl = cfg.FaviconUrl,
            DefaultShareImageUrl = cfg.DefaultShareImageUrl
        };
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var logoUrl = await fileStorageService.SaveImageAsync(LogoFile, "uploads/branding", HttpContext.RequestAborted) ?? Input.LogoUrl;
        var faviconUrl = await fileStorageService.SaveImageAsync(FaviconFile, "uploads/branding", HttpContext.RequestAborted) ?? Input.FaviconUrl;
        var shareImageUrl = await fileStorageService.SaveImageAsync(DefaultShareImageFile, "uploads/branding", HttpContext.RequestAborted) ?? Input.DefaultShareImageUrl;

        var rs = await service.SaveBrandingAsync(new BrandingUpsertRequest(logoUrl, faviconUrl, shareImageUrl), HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/Branding");
    }

    public sealed class InputModel
    {
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? DefaultShareImageUrl { get; set; }
    }
}
