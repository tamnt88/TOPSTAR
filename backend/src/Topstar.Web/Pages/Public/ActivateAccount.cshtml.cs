using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class ActivateAccountModel(IAccountActivationService accountActivationService) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = "Liên kết kích hoạt không hợp lệ.";

    public async Task OnGetAsync([FromQuery] string? token, CancellationToken cancellationToken)
    {
        var result = await accountActivationService.ActivateByTokenAsync(token ?? string.Empty, cancellationToken);
        IsSuccess = result.Success;
        Message = result.Message;
    }
}
