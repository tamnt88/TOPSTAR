using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class EmailAccountsModel(ISystemSettingsAdminService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? EditId { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public IReadOnlyList<EmailAccountItem> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
        if (EditId.HasValue && EditId.Value != Guid.Empty)
        {
            var item = await service.GetEmailAccountByIdAsync(EditId.Value, HttpContext.RequestAborted);
            if (item is not null)
            {
                Input = new InputModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    EmailAddress = item.EmailAddress,
                    DisplayName = item.DisplayName,
                    SmtpHost = item.SmtpHost,
                    SmtpPort = item.SmtpPort,
                    SmtpEnableSsl = item.SmtpEnableSsl,
                    IsDefaultSender = item.IsDefaultSender,
                    IsDefaultReceiver = item.IsDefaultReceiver,
                    IsActive = item.IsActive,
                    IsVisible = true
                };
            }
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var request = new EmailAccountUpsertRequest(
            Input.Id,
            Input.Name,
            Input.EmailAddress,
            Input.DisplayName,
            Input.SmtpHost,
            Input.SmtpPort,
            Input.SmtpUsername,
            Input.SmtpPassword,
            Input.SmtpEnableSsl,
            Input.ImapHost,
            Input.ImapPort,
            Input.ImapUsername,
            Input.ImapPassword,
            Input.ImapEnableSsl,
            Input.IsDefaultSender,
            Input.IsDefaultReceiver,
            Input.IsActive,
            Input.IsVisible);

        var rs = Input.Id.HasValue
            ? await service.UpdateEmailAccountAsync(request, HttpContext.RequestAborted)
            : await service.CreateEmailAccountAsync(request, HttpContext.RequestAborted);

        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/EmailAccounts");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var rs = await service.DeleteEmailAccountAsync(id, HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectToPage("/Admin/Settings/EmailAccounts");
    }

    private async Task LoadDataAsync() => Rows = await service.GetEmailAccountsAsync(HttpContext.RequestAborted);

    public sealed class InputModel
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public bool SmtpEnableSsl { get; set; } = true;
        public string? ImapHost { get; set; }
        public int? ImapPort { get; set; }
        public string? ImapUsername { get; set; }
        public string? ImapPassword { get; set; }
        public bool ImapEnableSsl { get; set; } = true;
        public bool IsDefaultSender { get; set; }
        public bool IsDefaultReceiver { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }
}
