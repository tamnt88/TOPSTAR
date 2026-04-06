using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Topstar.Application;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class RegisterModel(IAuthService authService, IKocRegistrationService kocRegistrationService) : AuthPageBase
{
    [BindProperty]
    public QuickKocInput KocInput { get; set; } = new();

    [BindProperty]
    public QuickBrandInput BrandInput { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string ActiveTab { get; private set; } = "koc";
    public List<SelectListItem> ProvinceOptions { get; private set; } = new();
    public List<SelectListItem> WardOptions { get; private set; } = new();

    public sealed class QuickKocInput
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        public string BirthDate { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class QuickBrandInput
    {
        [Display(Name = "Tên người đại diện")]
        [Required(ErrorMessage = "Vui lòng nhập tên người đại diện.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Tên công ty/Brand")]
        [Required(ErrorMessage = "Vui lòng nhập tên công ty/Brand.")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại liên hệ")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Số nhà + tên đường")]
        [Required(ErrorMessage = "Vui lòng nhập số nhà + tên đường.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Display(Name = "Tỉnh/Thành phố")]
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        public Guid? ProvinceId { get; set; }

        [Display(Name = "Phường/Xã")]
        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        public Guid? WardId { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? tab = null, CancellationToken cancellationToken = default)
    {
        ActiveTab = NormalizeTab(tab);
        await LoadLocationOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostQuickKocAsync(CancellationToken cancellationToken)
    {
        ActiveTab = "koc";
        if (!TryValidateModel(KocInput, nameof(KocInput)))
        {
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        if (!DateOnly.TryParseExact(KocInput.BirthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            ErrorMessage = "Ngày sinh không hợp lệ.";
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        var rs = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Koc,
                KocInput.Email,
                KocInput.Phone,
                KocInput.Gender,
                KocInput.Password,
                KocInput.FullName,
                null,
                KocInput.Phone,
                BuildDeviceContext()),
            cancellationToken);

        if (!rs.Success || rs.Data is null)
        {
            ErrorMessage = rs.Message;
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        await SignInUserAsync(rs.Data);
        return Redirect("/Koc/Dashboard?completeProfile=1");
    }

    public async Task<IActionResult> OnPostQuickBrandAsync(CancellationToken cancellationToken)
    {
        ActiveTab = "brand";
        if (!TryValidateModel(BrandInput, nameof(BrandInput)))
        {
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        if (BrandInput.ProvinceId is null || !await kocRegistrationService.IsValidProvinceAsync(BrandInput.ProvinceId.Value, cancellationToken))
        {
            ModelState.AddModelError("BrandInput.ProvinceId", "Tỉnh/Thành phố không hợp lệ.");
        }

        if (BrandInput.WardId is null || !await kocRegistrationService.IsValidWardAsync(BrandInput.WardId.Value, BrandInput.ProvinceId, cancellationToken))
        {
            ModelState.AddModelError("BrandInput.WardId", "Phường/Xã không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        var rs = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Brand,
                BrandInput.Email,
                BrandInput.Phone,
                null,
                BrandInput.Password,
                BrandInput.FullName,
                BrandInput.CompanyName,
                BrandInput.Phone,
                BuildDeviceContext(),
                BrandInput.StreetAddress,
                BrandInput.ProvinceId,
                BrandInput.WardId),
            cancellationToken);

        if (!rs.Success || rs.Data is null)
        {
            ErrorMessage = rs.Message;
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        await SignInUserAsync(rs.Data);
        return Redirect("/Brand/Dashboard?completeProfile=1");
    }

    private async Task SignInUserAsync(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    private static string NormalizeTab(string? tab)
        => string.Equals(tab, "brand", StringComparison.OrdinalIgnoreCase) ? "brand" : "koc";

    public async Task<IActionResult> OnGetWardsByProvinceAsync(Guid? provinceId, CancellationToken cancellationToken)
    {
        if (provinceId is null || provinceId == Guid.Empty)
        {
            return new JsonResult(new { data = Array.Empty<object>() });
        }

        var wardsFromService = await kocRegistrationService.GetActiveWardsByProvinceAsync(provinceId.Value, cancellationToken);
        var wards = wardsFromService.Select(x => new { id = x.Id, name = x.Name });
        return new JsonResult(new { data = wards });
    }

    private async Task LoadLocationOptionsAsync(CancellationToken cancellationToken)
    {
        var provinces = await kocRegistrationService.GetActiveProvincesAsync(cancellationToken);
        ProvinceOptions = provinces
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
            .ToList();

        WardOptions = new List<SelectListItem>();
        if (BrandInput.ProvinceId.HasValue && BrandInput.ProvinceId.Value != Guid.Empty)
        {
            var wards = await kocRegistrationService.GetActiveWardsByProvinceAsync(BrandInput.ProvinceId.Value, cancellationToken);
            WardOptions = wards
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToList();
        }
    }
}
