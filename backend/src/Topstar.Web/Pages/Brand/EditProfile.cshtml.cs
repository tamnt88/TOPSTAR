using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class EditProfileModel(
    IDashboardDataService dashboardDataService,
    IKocRegistrationService kocRegistrationService,
    TopstarDbContext dbContext,
    IWebHostEnvironment environment,
    ILogger<EditProfileModel> logger) : BrandGuardedPageModel(dashboardDataService)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? BusinessLicenseFile { get; set; }

    public List<SelectListItem> ProvinceOptions { get; private set; } = new();
    public List<SelectListItem> WardOptions { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: true);
        if (redirect is not null)
        {
            // allow profile pages
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return RedirectToPage("/Public/Login");
        }

        await LoadLookupAsync(cancellationToken);
        await LoadCurrentDataAsync(parsedUserId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return RedirectToPage("/Public/Login");
            }

            NormalizeInput();

            if (Input.ProvinceId.HasValue && !await kocRegistrationService.IsValidProvinceAsync(Input.ProvinceId.Value, cancellationToken))
            {
                ModelState.AddModelError(nameof(Input.ProvinceId), "Tỉnh/Thành phố không hợp lệ.");
            }

            if (Input.WardId.HasValue && !await kocRegistrationService.IsValidWardAsync(Input.WardId.Value, Input.ProvinceId, cancellationToken))
            {
                ModelState.AddModelError(nameof(Input.WardId), "Phường/Xã không hợp lệ.");
            }

            await LoadLookupAsync(cancellationToken);
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);
            var brand = await dbContext.BrandProfiles.FirstOrDefaultAsync(x => x.UserId == parsedUserId, cancellationToken);
            if (user is null || brand is null)
            {
                ErrorMessage = "Không tìm thấy hồ sơ Brand.";
                return Page();
            }

            if (BusinessLicenseFile is not null && BusinessLicenseFile.Length > 0)
            {
                var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "brand-license");
                Directory.CreateDirectory(uploadRoot);
                var ext = Path.GetExtension(BusinessLicenseFile.FileName);
                var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext;
                var fileName = $"{Guid.NewGuid():N}{safeExt}";
                var fullPath = Path.Combine(uploadRoot, fileName);

                await using var fs = System.IO.File.Create(fullPath);
                await BusinessLicenseFile.CopyToAsync(fs, cancellationToken);

                brand.BusinessLicenseFileUrl = $"/uploads/brand-license/{fileName}";
            }
            else if (string.IsNullOrWhiteSpace(brand.BusinessLicenseFileUrl))
            {
                ModelState.AddModelError(nameof(BusinessLicenseFile), "Vui lòng tải lên giấy phép kinh doanh.");
                return Page();
            }

            brand.TaxCode = Input.TaxCode;
            brand.TaxCompanyName = Input.TaxCompanyName;
            brand.TaxInvoiceAddress = Input.TaxInvoiceAddress;

            user.Email = Input.Email;
            user.Phone = Input.Phone;
            user.UpdatedAtUtc = DateTime.UtcNow;

            brand.CompanyName = Input.CompanyName;
            brand.ContactName = Input.ContactName;
            brand.ContactPhone = Input.ContactPhone;
            brand.StreetAddress = Input.StreetAddress;
            brand.ProvinceId = Input.ProvinceId;
            brand.WardId = Input.WardId;
            brand.Status = "Submitted";
            brand.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToPage("/Brand/Dashboard");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Brand profile update failed for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            await LoadLookupAsync(cancellationToken);
            ErrorMessage = $"Đã xảy ra lỗi khi lưu hồ sơ Brand: {ex.Message}";
            return Page();
        }
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

    private async Task LoadCurrentDataAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var brand = await dbContext.BrandProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null || brand is null)
        {
            return;
        }

        Input = new InputModel
        {
            CompanyName = brand.CompanyName,
            ContactName = brand.ContactName,
            ContactPhone = brand.ContactPhone,
            Email = user.Email,
            Phone = user.Phone ?? string.Empty,
            StreetAddress = brand.StreetAddress ?? string.Empty,
            ProvinceId = brand.ProvinceId,
            WardId = brand.WardId,
            TaxCode = brand.TaxCode ?? string.Empty,
            TaxCompanyName = brand.TaxCompanyName ?? string.Empty,
            TaxInvoiceAddress = brand.TaxInvoiceAddress ?? string.Empty,
            CurrentBusinessLicenseUrl = brand.BusinessLicenseFileUrl
        };

        if (Input.ProvinceId.HasValue && Input.ProvinceId.Value != Guid.Empty)
        {
            var wards = await kocRegistrationService.GetActiveWardsByProvinceAsync(Input.ProvinceId.Value, cancellationToken);
            WardOptions = wards.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();
        }
    }

    private async Task LoadLookupAsync(CancellationToken cancellationToken)
    {
        var provinces = await kocRegistrationService.GetActiveProvincesAsync(cancellationToken);
        ProvinceOptions = provinces.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();

        if (Input.ProvinceId.HasValue && Input.ProvinceId.Value != Guid.Empty)
        {
            var wards = await kocRegistrationService.GetActiveWardsByProvinceAsync(Input.ProvinceId.Value, cancellationToken);
            WardOptions = wards.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();
        }
        else
        {
            WardOptions = new List<SelectListItem>();
        }
    }

    private void NormalizeInput()
    {
        Input.CompanyName = (Input.CompanyName ?? string.Empty).Trim();
        Input.ContactName = (Input.ContactName ?? string.Empty).Trim();
        Input.ContactPhone = (Input.ContactPhone ?? string.Empty).Trim();
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Phone = (Input.Phone ?? string.Empty).Trim();
        Input.StreetAddress = (Input.StreetAddress ?? string.Empty).Trim();
        Input.TaxCode = (Input.TaxCode ?? string.Empty).Trim();
        Input.TaxCompanyName = (Input.TaxCompanyName ?? string.Empty).Trim();
        Input.TaxInvoiceAddress = (Input.TaxInvoiceAddress ?? string.Empty).Trim();
    }

    public sealed class InputModel
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string ContactName { get; set; } = string.Empty;

        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        public Guid? ProvinceId { get; set; }

        [Required]
        public Guid? WardId { get; set; }

        [Required]
        public string TaxCode { get; set; } = string.Empty;

        [Required]
        public string TaxCompanyName { get; set; } = string.Empty;

        [Required]
        public string TaxInvoiceAddress { get; set; } = string.Empty;

        public string? CurrentBusinessLicenseUrl { get; set; }
    }
}
