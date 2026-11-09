using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;
using Topstar.Web.Pages.Public;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class AccountSecurityModel(IAuthService authService) : AuthPageBase
{
    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostChangePasswordAsync(
        string currentPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(currentPassword))
            errors["currentPassword"] = "Vui lòng nhập mật khẩu hiện tại.";

        if (string.IsNullOrWhiteSpace(newPassword))
            errors["newPassword"] = "Vui lòng nhập mật khẩu mới.";
        else if (newPassword.Length is < 8 or > 128)
            errors["newPassword"] = "Mật khẩu mới phải từ 8 đến 128 ký tự.";
        else if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower) || !newPassword.Any(char.IsDigit))
            errors["newPassword"] = "Mật khẩu phải có chữ hoa, chữ thường và chữ số.";

        if (string.IsNullOrWhiteSpace(confirmPassword))
            errors["confirmPassword"] = "Vui lòng xác nhận mật khẩu mới.";
        else if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            errors["confirmPassword"] = "Xác nhận mật khẩu không khớp.";

        if (errors.Count > 0)
            return new JsonResult(new { success = false, message = "Vui lòng kiểm tra lại thông tin.", errors });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var result = await authService.ChangePasswordAsync(
            new ChangePasswordRequest(userId, currentPassword, newPassword, BuildDeviceContext()),
            cancellationToken);

        return new JsonResult(new
        {
            success = result.Success,
            message = result.Message,
            errors = result.Success ? null : new Dictionary<string, string> { ["currentPassword"] = result.Message }
        });
    }
}
