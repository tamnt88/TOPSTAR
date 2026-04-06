using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class RegisterBrandModel(IAuthService authService, IAuthRepository authRepository) : AuthPageBase
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công ty")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số nhà + tên đường")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phường/xã")]
        public string WardName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố")]
        public string CityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetCheckIdentityAsync(string? email, string? phone, CancellationToken cancellationToken)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedPhone = (phone ?? string.Empty).Trim();
        var emailExists = false;
        var phoneExists = false;

        if (!string.IsNullOrWhiteSpace(normalizedEmail) && new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            emailExists = await authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(normalizedPhone) && Regex.IsMatch(normalizedPhone, "^(0|\\+84)(3|5|7|8|9)[0-9]{8}$"))
        {
            phoneExists = await authRepository.PhoneExistsAsync(normalizedPhone, cancellationToken);
        }

        return new JsonResult(new { emailExists, phoneExists });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Phone = (Input.Phone ?? string.Empty).Trim();
        Input.ContactPhone = (Input.ContactPhone ?? string.Empty).Trim();
        Input.StreetAddress = (Input.StreetAddress ?? string.Empty).Trim();
        Input.WardName = (Input.WardName ?? string.Empty).Trim();
        Input.CityName = (Input.CityName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await authRepository.EmailExistsAsync(Input.Email.ToLowerInvariant(), cancellationToken))
        {
            ModelState.AddModelError("Input.Email", "Email đã tồn tại trong hệ thống.");
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.Phone) && await authRepository.PhoneExistsAsync(Input.Phone, cancellationToken))
        {
            ModelState.AddModelError("Input.Phone", "Số điện thoại đã tồn tại trong hệ thống.");
            return Page();
        }

        var result = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Brand,
                Input.Email,
                Input.Phone,
                null,
                Input.Password,
                Input.FullName,
                Input.CompanyName,
                Input.ContactPhone,
                BuildDeviceContext()),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        return Redirect("/Brand/Dashboard");
    }
}
