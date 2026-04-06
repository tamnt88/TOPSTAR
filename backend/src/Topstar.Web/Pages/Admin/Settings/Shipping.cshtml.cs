using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class ShippingModel(ISystemSettingsAdminService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? EditId { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public IReadOnlyList<ShippingConfigItem> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
        if (EditId.HasValue && EditId.Value != Guid.Empty)
        {
            var item = await service.GetShippingConfigByIdAsync(EditId.Value, HttpContext.RequestAborted);
            if (item is not null)
            {
                Input = new InputModel
                {
                    Id = item.Id,
                    ProviderCode = item.ProviderCode,
                    ProviderName = item.ProviderName,
                    ApiBaseUrl = item.ApiBaseUrl,
                    IsDefault = item.IsDefault,
                    IsSandbox = item.IsSandbox,
                    IsActive = item.IsActive,
                    IsVisible = true
                };
            }
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var request = new ShippingConfigUpsertRequest(
            Input.Id,
            Input.ProviderCode,
            Input.ProviderName,
            Input.ApiBaseUrl,
            Input.ApiToken,
            Input.ShopId,
            Input.ClientId,
            Input.ClientSecret,
            Input.WebhookUrl,
            Input.WebhookSecret,
            Input.IsSandbox,
            Input.IsDefault,
            Input.IsActive,
            Input.IsVisible);

        var rs = Input.Id.HasValue
            ? await service.UpdateShippingConfigAsync(request, HttpContext.RequestAborted)
            : await service.CreateShippingConfigAsync(request, HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/Shipping");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var rs = await service.DeleteShippingConfigAsync(id, HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/Shipping");
    }

    private async Task LoadDataAsync() => Rows = await service.GetShippingConfigsAsync(HttpContext.RequestAborted);

    public sealed class InputModel
    {
        public Guid? Id { get; set; }
        public string ProviderCode { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string? ApiToken { get; set; }
        public string? ShopId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? WebhookUrl { get; set; }
        public string? WebhookSecret { get; set; }
        public bool IsSandbox { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
