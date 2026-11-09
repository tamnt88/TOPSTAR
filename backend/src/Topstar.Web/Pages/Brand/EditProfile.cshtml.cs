using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class EditProfileModel(
    IProfileAccessService profileAccessService,
    IKocRegistrationService kocRegistrationService,
    TopstarDbContext dbContext,
    IWebHostEnvironment environment,
    IFileStorageService fileStorageService,
    ILogger<EditProfileModel> logger) : BrandGuardedPageModel(profileAccessService)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? BusinessLicenseFile { get; set; }

    [BindProperty]
    public IFormFile? LogoFile { get; set; }

    public List<SelectListItem> ProvinceOptions { get; private set; } = new();
    public List<SelectListItem> WardOptions { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public BrandCompletionView Completion { get; private set; } = new();

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
        Completion = BuildCompletion(await ProfileAccessService.CheckBrandAsync(parsedUserId, cancellationToken));
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
            Input.Industries = Input.Industries.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
            var validIndustries = await dbContext.KocCategories.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken);
            if (Input.Industries.Any(x => !validIndustries.Contains(x)))
                ModelState.AddModelError("Input.Industries", "Vui lòng chọn ngành hàng trong danh mục đang hiển thị.");
            Input.Industry = string.Join("; ", Input.Industries);
            if (Input.Industry.Length > 100)
            {
                var connection = dbContext.Database.GetDbConnection();
                var close = connection.State != System.Data.ConnectionState.Open;
                try
                {
                    if (close) await connection.OpenAsync(cancellationToken);
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COL_LENGTH('dbo.BrandProfiles','Industry')";
                    var bytes = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
                    if (bytes != -1 && Input.Industry.Length * 2 > bytes)
                        ModelState.AddModelError("Input.Industries", "Cần cập nhật SQL 156 để lưu danh sách nhiều ngành hàng.");
                }
                finally { if (close) await connection.CloseAsync(); }
            }

            if (!string.IsNullOrWhiteSpace(Input.RefundBankName)
                || !string.IsNullOrWhiteSpace(Input.RefundAccountNumber)
                || !string.IsNullOrWhiteSpace(Input.RefundAccountHolder))
            {
                if (string.IsNullOrWhiteSpace(Input.RefundBankName))
                    ModelState.AddModelError("Input.RefundBankName", "Vui lòng nhập ngân hàng nhận hoàn tiền.");
                if (string.IsNullOrWhiteSpace(Input.RefundAccountNumber))
                    ModelState.AddModelError("Input.RefundAccountNumber", "Vui lòng nhập số tài khoản nhận hoàn tiền.");
                if (string.IsNullOrWhiteSpace(Input.RefundAccountHolder))
                    ModelState.AddModelError("Input.RefundAccountHolder", "Vui lòng nhập chủ tài khoản nhận hoàn tiền.");
            }

            if (Input.ProvinceId.HasValue && !await kocRegistrationService.IsValidProvinceAsync(Input.ProvinceId.Value, cancellationToken))
            {
                ModelState.AddModelError("Input.ProvinceId", "Tỉnh/Thành phố không hợp lệ.");
            }

            if (Input.WardId.HasValue && !await kocRegistrationService.IsValidWardAsync(Input.WardId.Value, Input.ProvinceId, cancellationToken))
            {
                ModelState.AddModelError("Input.WardId", "Phường/Xã không hợp lệ.");
            }

            await LoadLookupAsync(cancellationToken);
            if (!ModelState.IsValid)
            {
                await LoadCompletionAsync(parsedUserId, cancellationToken);
                return Page();
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);
            var brand = await dbContext.BrandProfiles.FirstOrDefaultAsync(x => x.UserId == parsedUserId, cancellationToken);
            if (user is null || brand is null)
            {
                ErrorMessage = "Không tìm thấy hồ sơ Brand.";
                await LoadCompletionAsync(parsedUserId, cancellationToken);
                return Page();
            }

            await ValidateUniqueLoginFieldsAsync(parsedUserId, cancellationToken);
            if (!ModelState.IsValid)
            {
                await LoadCompletionAsync(parsedUserId, cancellationToken);
                return Page();
            }

            if (BusinessLicenseFile is not null && BusinessLicenseFile.Length > 0)
            {
                ValidateBusinessLicense();
                if (!ModelState.IsValid)
                {
                    await LoadCompletionAsync(parsedUserId, cancellationToken);
                    return Page();
                }
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
                await LoadCompletionAsync(parsedUserId, cancellationToken);
                return Page();
            }

            brand.TaxCode = Input.TaxCode;
            brand.TaxCompanyName = Input.TaxCompanyName;
            brand.TaxInvoiceAddress = Input.TaxInvoiceAddress;
            brand.RefundBankName = Input.RefundBankName?.Trim();
            brand.RefundAccountNumber = Input.RefundAccountNumber?.Trim();
            brand.RefundAccountHolder = Input.RefundAccountHolder?.Trim();

            user.Email = Input.Email;
            user.Phone = Input.Phone;
            user.UpdatedAtUtc = DateTime.UtcNow;

            brand.CompanyName = Input.CompanyName;
            brand.ContactName = Input.ContactName;
            brand.ContactPhone = Input.ContactPhone;
            brand.Industry = Input.Industry;
            brand.Website = Input.Website;
            if (LogoFile is not null && LogoFile.Length > 0)
            {
                if (LogoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(LogoFile), "Logo không được vượt quá 5 MB.");
                    await LoadCompletionAsync(parsedUserId, cancellationToken);
                    return Page();
                }

                var logoUrl = await fileStorageService.SaveImageAsync(LogoFile, "uploads/brand-logo", cancellationToken);
                if (string.IsNullOrWhiteSpace(logoUrl))
                {
                    ModelState.AddModelError(nameof(LogoFile), "Logo chỉ hỗ trợ JPG, PNG, WEBP hoặc SVG.");
                    await LoadCompletionAsync(parsedUserId, cancellationToken);
                    return Page();
                }
                brand.LogoUrl = logoUrl;
            }
            brand.StreetAddress = Input.StreetAddress;
            brand.ProvinceId = Input.ProvinceId;
            brand.WardId = Input.WardId;
            brand.Status = "PendingReview";
            brand.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToPage("/Brand/Dashboard");
        }
        catch (DbUpdateException dbEx) when (TryMapUniqueViolationToModelState(dbEx))
        {
            await LoadLookupAsync(cancellationToken);
            if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId))
                await LoadCompletionAsync(parsedUserId, cancellationToken);
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Brand profile update failed for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            await LoadLookupAsync(cancellationToken);
            if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId))
                await LoadCompletionAsync(parsedUserId, cancellationToken);
            ErrorMessage = "Hệ thống chưa thể lưu hồ sơ Brand. Vui lòng thử lại; lỗi đã được ghi nhận để kiểm tra.";
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
            Industry = brand.Industry ?? string.Empty,
            Industries = (brand.Industry ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Website = brand.Website ?? string.Empty,
            CurrentLogoUrl = brand.LogoUrl,
            Email = user.Email,
            Phone = user.Phone ?? string.Empty,
            StreetAddress = brand.StreetAddress ?? string.Empty,
            ProvinceId = brand.ProvinceId,
            WardId = brand.WardId,
            TaxCode = brand.TaxCode ?? string.Empty,
            TaxCompanyName = brand.TaxCompanyName ?? string.Empty,
            TaxInvoiceAddress = brand.TaxInvoiceAddress ?? string.Empty,
            RefundBankName = brand.RefundBankName ?? string.Empty,
            RefundAccountNumber = brand.RefundAccountNumber ?? string.Empty,
            RefundAccountHolder = brand.RefundAccountHolder ?? string.Empty,
            CurrentBusinessLicenseUrl = brand.BusinessLicenseFileUrl
        };

        if (Input.ProvinceId.HasValue && Input.ProvinceId.Value != Guid.Empty)
        {
            var wards = await kocRegistrationService.GetActiveWardsByProvinceAsync(Input.ProvinceId.Value, cancellationToken);
            WardOptions = wards.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();
        }
    }

    public List<SelectListItem> IndustryOptions { get; private set; } = [];

    private async Task LoadLookupAsync(CancellationToken cancellationToken)
    {
        IndustryOptions = await dbContext.KocCategories.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Name, Text = x.Name }).ToListAsync(cancellationToken);
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
        Input.Industry = (Input.Industry ?? string.Empty).Trim();
        Input.Website = (Input.Website ?? string.Empty).Trim();
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Phone = (Input.Phone ?? string.Empty).Trim();
        Input.StreetAddress = (Input.StreetAddress ?? string.Empty).Trim();
        Input.TaxCode = (Input.TaxCode ?? string.Empty).Trim();
        Input.TaxCompanyName = (Input.TaxCompanyName ?? string.Empty).Trim();
        Input.TaxInvoiceAddress = (Input.TaxInvoiceAddress ?? string.Empty).Trim();
    }

    private async Task LoadCompletionAsync(Guid userId, CancellationToken cancellationToken)
        => Completion = BuildCompletion(await ProfileAccessService.CheckBrandAsync(userId, cancellationToken));

    private void ValidateBusinessLicense()
    {
        if (BusinessLicenseFile is null || BusinessLicenseFile.Length == 0) return;
        const long maximumFileSize = 10 * 1024 * 1024;
        var extension = Path.GetExtension(BusinessLicenseFile.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };
        if (BusinessLicenseFile.Length > maximumFileSize)
            ModelState.AddModelError(nameof(BusinessLicenseFile), "Giấy phép kinh doanh không được vượt quá 10 MB.");
        if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(BusinessLicenseFile.ContentType))
            ModelState.AddModelError(nameof(BusinessLicenseFile), "Giấy phép kinh doanh chỉ hỗ trợ PDF, JPG hoặc PNG.");
    }

    private async Task ValidateUniqueLoginFieldsAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var email = (Input.Email ?? string.Empty).Trim();
        var phone = (Input.Phone ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailExists = await dbContext.Users.AsNoTracking()
                .AnyAsync(x => x.Id != currentUserId && x.Email == email, cancellationToken);
            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email đã được sử dụng bởi tài khoản khác.");
            }
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var phoneExists = await dbContext.Users.AsNoTracking()
                .AnyAsync(x => x.Id != currentUserId && x.Phone == phone, cancellationToken);
            if (phoneExists)
            {
                ModelState.AddModelError("Input.Phone", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
            }
        }
    }

    private bool TryMapUniqueViolationToModelState(DbUpdateException ex)
    {
        var rootMessage = ex.GetBaseException().Message;
        if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            if (rootMessage.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.Email", "Email đã được sử dụng bởi tài khoản khác.");
                return true;
            }

            if (rootMessage.Contains("Phone", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.Phone", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
                return true;
            }
        }

        return false;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên công ty/Brand.")]
        [StringLength(200, ErrorMessage = "Tên công ty/Brand không được vượt quá 200 ký tự.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên người đại diện.")]
        [StringLength(150, ErrorMessage = "Tên người đại diện không được vượt quá 150 ký tự.")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người liên hệ.")]
        [RegularExpression(@"^(?:\+84|0)\d{9,10}$", ErrorMessage = "Số điện thoại liên hệ không đúng định dạng Việt Nam.")]
        public string ContactPhone { get; set; } = string.Empty;

        public List<string> Industries { get; set; } = [];
        [StringLength(2000, ErrorMessage = "Danh sách ngành hàng không được vượt quá 2.000 ký tự.")]
        public string Industry { get; set; } = string.Empty;

        [Url(ErrorMessage = "Website phải là địa chỉ hợp lệ, ví dụ https://example.com.")]
        [StringLength(300, ErrorMessage = "Website không được vượt quá 300 ký tự.")]
        public string Website { get; set; } = string.Empty;

        public string? CurrentLogoUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email liên hệ.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại user.")]
        [RegularExpression(@"^(?:\+84|0)\d{9,10}$", ErrorMessage = "Số điện thoại tài khoản không đúng định dạng Việt Nam.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số nhà và tên đường.")]
        [StringLength(300, ErrorMessage = "Địa chỉ doanh nghiệp không được vượt quá 300 ký tự.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
        public Guid? ProvinceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
        public Guid? WardId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã số thuế.")]
        [RegularExpression(@"^\d{10}(?:-\d{3})?$", ErrorMessage = "Mã số thuế phải gồm 10 chữ số hoặc dạng 10 chữ số-3 chữ số.")]
        public string TaxCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên pháp lý trên mã số thuế.")]
        [StringLength(250, ErrorMessage = "Tên pháp lý không được vượt quá 250 ký tự.")]
        public string TaxCompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ xuất hóa đơn.")]
        [StringLength(500, ErrorMessage = "Địa chỉ xuất hóa đơn không được vượt quá 500 ký tự.")]
        public string TaxInvoiceAddress { get; set; } = string.Empty;

        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên ngân hàng phải từ 2 đến 200 ký tự.")]
        public string? RefundBankName { get; set; }

        [StringLength(100, MinimumLength = 4, ErrorMessage = "Số tài khoản phải từ 4 đến 100 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9 -]+$", ErrorMessage = "Số tài khoản chỉ được chứa chữ, số, khoảng trắng hoặc dấu gạch ngang.")]
        public string? RefundAccountNumber { get; set; }

        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên chủ tài khoản phải từ 2 đến 200 ký tự.")]
        public string? RefundAccountHolder { get; set; }

        public string? CurrentBusinessLicenseUrl { get; set; }
    }

    public sealed class BrandCompletionView
    {
        public int Percent { get; init; }
        public bool IsComplete { get; init; }
        public bool IsApproved { get; init; }
        public string StatusLabel { get; init; } = "Đang cập nhật";
        public IReadOnlyList<BrandCompletionItem> MissingItems { get; init; } = [];
    }

    public sealed record BrandCompletionItem(string Label, string Detail, string Pane, string TargetId, string Icon);

    private static BrandCompletionView BuildCompletion(ProfileAccessResult access)
    {
        const int requiredFieldCount = 12;
        var fieldMap = new Dictionary<string, BrandCompletionItem>(StringComparer.OrdinalIgnoreCase)
        {
            ["BrandProfile"] = new("Khởi tạo hồ sơ Brand", "Hồ sơ doanh nghiệp chưa tồn tại.", "brand-company-pane", "Input_CompanyName", "fa-building"),
            ["Email"] = new("Email liên hệ", "Nhập email nhận thông báo và đăng nhập.", "brand-company-pane", "Input_Email", "fa-envelope"),
            ["Phone"] = new("Số điện thoại tài khoản", "Nhập số điện thoại của tài khoản.", "brand-company-pane", "Input_Phone", "fa-phone"),
            ["CompanyName"] = new("Tên công ty/Brand", "Nhập tên thương hiệu hiển thị trên hệ thống.", "brand-company-pane", "Input_CompanyName", "fa-building"),
            ["ContactName"] = new("Người đại diện", "Nhập họ tên người đại diện Brand.", "brand-company-pane", "Input_ContactName", "fa-user-tie"),
            ["ContactPhone"] = new("Số điện thoại liên hệ", "Nhập số điện thoại người phụ trách.", "brand-company-pane", "Input_ContactPhone", "fa-address-book"),
            ["StreetAddress"] = new("Địa chỉ doanh nghiệp", "Nhập số nhà và tên đường.", "brand-company-pane", "Input_StreetAddress", "fa-location-dot"),
            ["ProvinceId"] = new("Tỉnh/Thành phố", "Chọn tỉnh hoặc thành phố của doanh nghiệp.", "brand-company-pane", "brand-edit-province", "fa-map"),
            ["WardId"] = new("Phường/Xã", "Chọn phường hoặc xã tương ứng.", "brand-company-pane", "brand-edit-ward", "fa-map-pin"),
            ["TaxCode"] = new("Mã số thuế", "Nhập mã số thuế đang hoạt động.", "brand-legal-pane", "Input_TaxCode", "fa-receipt"),
            ["TaxCompanyName"] = new("Tên pháp lý", "Nhập tên pháp lý đúng theo mã số thuế.", "brand-legal-pane", "Input_TaxCompanyName", "fa-scale-balanced"),
            ["TaxInvoiceAddress"] = new("Địa chỉ xuất hóa đơn", "Nhập địa chỉ dùng để xuất hóa đơn.", "brand-legal-pane", "Input_TaxInvoiceAddress", "fa-file-invoice"),
            ["BusinessLicenseFileUrl"] = new("Giấy phép kinh doanh", "Tải bản PDF hoặc ảnh rõ nét.", "brand-legal-pane", "BusinessLicenseFile", "fa-file-shield")
        };

        var missingItems = access.MissingFields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(field => fieldMap.TryGetValue(field, out var item)
                ? item
                : new BrandCompletionItem(field, "Bổ sung thông tin bắt buộc này.", "brand-company-pane", "Input_CompanyName", "fa-circle-exclamation"))
            .ToList();
        var missingCount = access.MissingFields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Sum(field => field.Equals("BrandProfile", StringComparison.OrdinalIgnoreCase) ? requiredFieldCount : 1);
        var percent = access.Exists
            ? Math.Clamp((int)Math.Round((requiredFieldCount - Math.Min(requiredFieldCount, missingCount)) * 100m / requiredFieldCount), 0, 100)
            : 0;
        var statusLabel = access.Status.ToLowerInvariant() switch
        {
            "approved" => "Đã duyệt",
            "pendingreview" => "Đang chờ duyệt",
            "needupdate" => "Cần bổ sung",
            "rejected" => "Đã từ chối",
            "locked" => "Đã khóa",
            "draft" => "Bản nháp",
            _ => "Đang cập nhật"
        };

        return new BrandCompletionView
        {
            Percent = percent,
            IsComplete = access.IsComplete,
            IsApproved = access.IsApproved,
            StatusLabel = statusLabel,
            MissingItems = missingItems
        };
    }
}
