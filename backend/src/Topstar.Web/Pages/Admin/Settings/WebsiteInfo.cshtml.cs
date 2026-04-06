using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class WebsiteInfoModel(ISystemSettingsAdminService service) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var cfg = await service.GetWebsiteConfigAsync(HttpContext.RequestAborted);
        if (cfg is null) return;
        Input = new InputModel
        {
            WebsiteName = cfg.WebsiteName,
            ShortName = cfg.ShortName,
            Domain = cfg.Domain,
            Address = cfg.Address,
            ContactEmail = cfg.ContactEmail,
            Hotline = cfg.Hotline,
            CopyrightText = cfg.CopyrightText,
            IsActive = cfg.IsActive,
            IsVisible = cfg.IsVisible
        };
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var rs = await service.SaveWebsiteConfigAsync(new WebsiteConfigUpsertRequest(
            Input.WebsiteName, Input.ShortName, Input.Domain, Input.Address, Input.ContactEmail, Input.Hotline, Input.CopyrightText, Input.IsActive, Input.IsVisible), HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/WebsiteInfo");
    }

    public sealed class InputModel
    {
        public string WebsiteName { get; set; } = "TopStar";
        public string? ShortName { get; set; }
        public string? Domain { get; set; }
        public string? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? Hotline { get; set; }
        public string? CopyrightText { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
