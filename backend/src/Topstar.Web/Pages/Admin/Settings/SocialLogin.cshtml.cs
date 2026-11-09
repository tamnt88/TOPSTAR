using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class SocialLoginModel(ISystemSettingsAdminService service) : PageModel
{
    [BindProperty] public ProviderInput Google { get; set; } = new() { Provider = "Google" };
    [BindProperty] public ProviderInput Facebook { get; set; } = new() { Provider = "Facebook" };

    public sealed class ProviderInput
    {
        public string Provider { get; set; } = string.Empty;
        [StringLength(1000)] public string? RedirectUri { get; set; }
        [StringLength(500)] public string? ClientId { get; set; }
        [StringLength(1000)] public string? ClientSecret { get; set; }
        public bool LoginEnabled { get; set; }
        public bool RegistrationEnabled { get; set; }
        public bool BrandEnabled { get; set; } = true;
        public bool KocEnabled { get; set; } = true;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Google = Map(await service.GetSocialLoginConfigurationAsync("Google", cancellationToken));
        Facebook = Map(await service.GetSocialLoginConfigurationAsync("Facebook", cancellationToken));
    }

    public Task<IActionResult> OnPostGoogleAsync(CancellationToken cancellationToken)
    {
        Google.Provider = "Google";
        return SaveAsync(Google, cancellationToken);
    }

    public Task<IActionResult> OnPostFacebookAsync(CancellationToken cancellationToken)
    {
        Facebook.Provider = "Facebook";
        return SaveAsync(Facebook, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(ProviderInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(cancellationToken);
            return Page();
        }

        var actor = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
        var result = await service.SaveSocialLoginConfigurationAsync(
            new(input.Provider, input.RedirectUri, input.ClientId, input.ClientSecret, input.LoginEnabled, input.RegistrationEnabled, input.BrandEnabled, input.KocEnabled),
            actor,
            cancellationToken);
        TempData[result.Success ? "AdminMessageSuccess" : "AdminMessageError"] = result.Message;
        return RedirectToPage();
    }

    private static ProviderInput Map(SocialLoginConfigurationItem item) => new()
    {
        Provider = item.Provider,
        RedirectUri = item.RedirectUri,
        ClientId = item.ClientId,
        ClientSecret = item.ClientSecret,
        LoginEnabled = item.LoginEnabled,
        RegistrationEnabled = item.RegistrationEnabled,
        BrandEnabled = item.BrandEnabled,
        KocEnabled = item.KocEnabled
    };

}
