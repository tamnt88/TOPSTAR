using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Topstar.Application;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class RegisterKocStep1Model(IAuthRepository authRepository, IKocRegistrationService kocRegistrationService) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    private const string Step1TempDataKey = "RegisterKocStep1";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> ProvinceOptions { get; private set; } = new();
    public List<SelectListItem> WardOptions { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var cached = TempData.Peek(Step1TempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var restored = JsonSerializer.Deserialize<InputModel>(cached);
            if (restored is not null)
            {
                Input = restored;
            }
        }

        await LoadLocationOptionsAsync(cancellationToken);
    }

    public sealed class InputModel
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(150, ErrorMessage = "Họ và tên tối đa 150 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression("^(0|\\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Giới tính không hợp lệ")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Số nhà + tên đường")]
        [Required(ErrorMessage = "Vui lòng nhập số nhà + tên đường")]
        [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự")]
        public string StreetAddress { get; set; } = string.Empty;

        [Display(Name = "Tỉnh/Thành phố")]
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public Guid? ProvinceId { get; set; }

        [Display(Name = "Phường/Xã")]
        [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
        public Guid? WardId { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string Password { get; set; } = string.Empty;
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.FullName = (Input.FullName ?? string.Empty).Trim();
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Phone = (Input.Phone ?? string.Empty).Trim();
        Input.Gender = (Input.Gender ?? string.Empty).Trim();
        Input.StreetAddress = (Input.StreetAddress ?? string.Empty).Trim();
        Input.Password ??= string.Empty;

        if (!string.IsNullOrWhiteSpace(Input.Password))
        {
            var hasLetter = Regex.IsMatch(Input.Password, "[A-Za-z]");
            var hasDigit = Regex.IsMatch(Input.Password, "[0-9]");
            if (!hasLetter || !hasDigit)
            {
                ModelState.AddModelError("Input.Password", "Mật khẩu phải có ít nhất 1 chữ và 1 số.");
            }
        }

        if (Input.BirthDate.HasValue)
        {
            if (Input.BirthDate.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("Input.BirthDate", "Ngày sinh không được ở tương lai.");
            }
            else
            {
                var age = DateTime.Today.Year - Input.BirthDate.Value.Year;
                if (Input.BirthDate.Value.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }

                if (age < 16)
                {
                    ModelState.AddModelError("Input.BirthDate", "Bạn phải từ 16 tuổi trở lên để đăng ký.");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.Email) &&
            await authRepository.EmailExistsAsync(Input.Email.ToLowerInvariant(), cancellationToken))
        {
            ModelState.AddModelError("Input.Email", "Email đã tồn tại trong hệ thống.");
        }

        if (!string.IsNullOrWhiteSpace(Input.Phone) &&
            await authRepository.PhoneExistsAsync(Input.Phone, cancellationToken))
        {
            ModelState.AddModelError("Input.Phone", "Số điện thoại đã tồn tại trong hệ thống.");
        }

        if (Input.ProvinceId.HasValue)
        {
            var provinceExists = await kocRegistrationService.IsValidProvinceAsync(Input.ProvinceId.Value, cancellationToken);
            if (!provinceExists)
            {
                ModelState.AddModelError("Input.ProvinceId", "Tỉnh/Thành phố không hợp lệ.");
            }
        }

        if (Input.WardId.HasValue)
        {
            var wardExists = await kocRegistrationService.IsValidWardAsync(Input.WardId.Value, Input.ProvinceId, cancellationToken);
            if (!wardExists)
            {
                ModelState.AddModelError("Input.WardId", "Phường/Xã không hợp lệ.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadLocationOptionsAsync(cancellationToken);
            return Page();
        }

        TempData[Step1TempDataKey] = JsonSerializer.Serialize(Input);
        return RedirectToPage("/Public/RegisterKocStep2");
    }

    private async Task LoadLocationOptionsAsync(CancellationToken cancellationToken)
    {
        var provinces = await kocRegistrationService.GetActiveProvincesAsync(cancellationToken);
        ProvinceOptions = provinces
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
            .ToList();

        WardOptions = new List<SelectListItem>();
        if (Input.ProvinceId.HasValue && Input.ProvinceId.Value != Guid.Empty)
        {
            var wards = await kocRegistrationService.GetActiveWardsByProvinceAsync(Input.ProvinceId.Value, cancellationToken);
            WardOptions = wards
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToList();
        }
    }
}
