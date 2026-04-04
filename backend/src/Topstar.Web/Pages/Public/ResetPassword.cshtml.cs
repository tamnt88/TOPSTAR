using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class ResetPasswordModel(IAuthService authService) : AuthPageBase
{
    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return RedirectToPage("/Public/ForgotPassword");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Thiếu token đặt lại mật khẩu.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await authService.ResetPasswordAsync(
            new ResetPasswordRequest(Token, Input.NewPassword, BuildDeviceContext()),
            cancellationToken);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        SuccessMessage = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập lại.";
        return Page();
    }
}
