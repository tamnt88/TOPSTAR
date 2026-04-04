using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class ForgotPasswordModel(IAuthService authService) : AuthPageBase
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string? ResetUrl { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại")]
        public string EmailOrPhone { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await authService.RequestPasswordResetAsync(
            new ForgotPasswordRequest(Input.EmailOrPhone, BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        ResetUrl = Url.Page("/Public/ResetPassword", null, new { token = result.Data.ResetToken }, Request.Scheme);
        return Page();
    }
}
