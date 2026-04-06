using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class EditProfileModel(
    IDashboardDataService dashboardDataService,
    IKocRegistrationService kocRegistrationService,
    TopstarDbContext dbContext,
    ILogger<EditProfileModel> logger) : KocGuardedPageModel(dashboardDataService)
{
    [TempData]
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "basic";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> ProvinceOptions { get; private set; } = new();
    public List<SelectListItem> WardOptions { get; private set; } = new();
    public List<SelectListItem> CategoryOptions { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<string> Platforms { get; } = ["TikTok", "Facebook", "Instagram", "YouTube"];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: true);
        if (redirect is not null)
        {
            return redirect;
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

    public async Task<IActionResult> OnPostAsync(string? saveSection, CancellationToken cancellationToken)
    {
        try
        {
            var section = NormalizeSection(saveSection);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return RedirectToPage("/Public/Login");
            }

            NormalizeInput();
            ModelState.Clear();

            if (section == "basic")
            {
                if (string.IsNullOrWhiteSpace(Input.FullName))
                {
                    ModelState.AddModelError("Input.FullName", "Vui lòng nhập họ và tên.");
                }
                if (string.IsNullOrWhiteSpace(Input.Email))
                {
                    ModelState.AddModelError("Input.Email", "Vui lòng nhập email.");
                }
                else if (!new EmailAddressAttribute().IsValid(Input.Email))
                {
                    ModelState.AddModelError("Input.Email", "Email không đúng định dạng.");
                }
                if (string.IsNullOrWhiteSpace(Input.Phone))
                {
                    ModelState.AddModelError("Input.Phone", "Vui lòng nhập số điện thoại.");
                }
                if (string.IsNullOrWhiteSpace(Input.Gender))
                {
                    ModelState.AddModelError("Input.Gender", "Vui lòng chọn giới tính.");
                }
                if (!Input.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError("Input.DateOfBirth", "Vui lòng chọn ngày sinh.");
                }
                if (string.IsNullOrWhiteSpace(Input.StreetAddress))
                {
                    ModelState.AddModelError("Input.StreetAddress", "Vui lòng nhập số nhà và tên đường.");
                }
                if (!Input.ProvinceId.HasValue)
                {
                    ModelState.AddModelError("Input.ProvinceId", "Vui lòng chọn Tỉnh/Thành phố.");
                }
                if (!Input.WardId.HasValue)
                {
                    ModelState.AddModelError("Input.WardId", "Vui lòng chọn Phường/Xã.");
                }

                if (Input.ProvinceId.HasValue && !await kocRegistrationService.IsValidProvinceAsync(Input.ProvinceId.Value, cancellationToken))
                {
                    ModelState.AddModelError("Input.ProvinceId", "Tỉnh/Thành phố không hợp lệ.");
                }

                if (Input.WardId.HasValue && !await kocRegistrationService.IsValidWardAsync(Input.WardId.Value, Input.ProvinceId, cancellationToken))
                {
                    ModelState.AddModelError("Input.WardId", "Phường/Xã không hợp lệ.");
                }
            }

            if (section == "social")
            {
                var hasAnyChannel = false;
                for (var i = 0; i < Input.Platforms.Count; i++)
                {
                    var item = Input.Platforms[i];
                    var hasChannel = !string.IsNullOrWhiteSpace(item.ChannelUrl);
                    if (hasChannel)
                    {
                        hasAnyChannel = true;

                        if (!Uri.TryCreate(item.ChannelUrl, UriKind.Absolute, out var channelUri)
                            || (channelUri.Scheme != Uri.UriSchemeHttp && channelUri.Scheme != Uri.UriSchemeHttps))
                        {
                            ModelState.AddModelError($"Input.Platforms[{i}].ChannelUrl", "Link kênh không đúng định dạng (http/https).");
                        }

                        if (!item.Followers.HasValue || item.Followers.Value <= 0)
                        {
                            ModelState.AddModelError($"Input.Platforms[{i}].Followers", "Vui lòng nhập followers lớn hơn 0.");
                        }

                        if (item.SelectedCategoryCodes.Count == 0)
                        {
                            ModelState.AddModelError($"Input.Platforms[{i}].SelectedCategoryCodes", "Vui lòng chọn ít nhất 1 ngành hàng cho kênh này.");
                        }
                    }
                    else if (item.Followers.HasValue && item.Followers.Value > 0)
                    {
                        ModelState.AddModelError($"Input.Platforms[{i}].ChannelUrl", "Vui lòng nhập link kênh khi đã khai báo followers.");
                    }
                }

                if (!hasAnyChannel)
                {
                    ModelState.AddModelError("Input.Platforms", "Vui lòng nhập ít nhất 1 kênh mạng xã hội.");
                }
            }

            await LoadLookupAsync(cancellationToken);
            if (!ModelState.IsValid)
            {
                ActiveTab = section;
                return Page();
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);
            var kocProfile = await dbContext.KocProfiles.FirstOrDefaultAsync(x => x.UserId == parsedUserId, cancellationToken);
            if (user is null || kocProfile is null)
            {
                ErrorMessage = "Không tìm thấy hồ sơ KOC.";
                return Page();
            }

            if (section == "basic")
            {
                await ValidateUniqueLoginFieldsAsync(parsedUserId, cancellationToken);
                if (!ModelState.IsValid)
                {
                    ActiveTab = section;
                    return Page();
                }
                user.Email = Input.Email.Trim();
                user.Phone = Input.Phone.Trim();
                user.Gender = Input.Gender;
                user.DateOfBirth = Input.DateOfBirth;
                user.UpdatedAtUtc = DateTime.UtcNow;

                kocProfile.DisplayName = Input.FullName.Trim();
                kocProfile.StreetAddress = Input.StreetAddress.Trim();
                kocProfile.ProvinceId = Input.ProvinceId;
                kocProfile.WardId = Input.WardId;
                kocProfile.Status = "PendingReview";
                kocProfile.UpdatedAtUtc = DateTime.UtcNow;
            }
            else if (section == "social")
            {
                var existingSocial = await dbContext.KocSocialAccounts.Where(x => x.KocProfileId == kocProfile.Id).ToListAsync(cancellationToken);
                dbContext.KocSocialAccounts.RemoveRange(existingSocial);

                var existingProfileCategories = await dbContext.KocProfileCategories.Where(x => x.KocProfileId == kocProfile.Id).ToListAsync(cancellationToken);
                dbContext.KocProfileCategories.RemoveRange(existingProfileCategories);

                var categoryMap = await dbContext.KocCategories.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
                var allCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var social in Input.Platforms)
                {
                    if (!string.IsNullOrWhiteSpace(social.ChannelUrl))
                    {
                        dbContext.KocSocialAccounts.Add(new KocSocialAccountEntity
                        {
                            Id = Guid.NewGuid(),
                            KocProfileId = kocProfile.Id,
                            Platform = social.Platform,
                            ChannelUrl = social.ChannelUrl.Trim(),
                            Followers = social.Followers ?? 0,
                            IsPrimary = social.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase),
                            IsVerified = false,
                            ViewCount = 0,
                            IsVisible = true,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }

                    foreach (var code in social.SelectedCategoryCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        allCodes.Add(code.Trim());
                    }
                }

                foreach (var code in allCodes)
                {
                    if (categoryMap.TryGetValue(code, out var categoryId))
                    {
                        dbContext.KocProfileCategories.Add(new KocProfileCategoryEntity
                        {
                            KocProfileId = kocProfile.Id,
                            CategoryId = categoryId,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }
            else
            {
                var bodyProfile = await dbContext.KocBodyProfiles.FirstOrDefaultAsync(x => x.KocProfileId == kocProfile.Id, cancellationToken);
                if (bodyProfile is null)
                {
                    bodyProfile = new KocBodyProfileEntity
                    {
                        Id = Guid.NewGuid(),
                        KocProfileId = kocProfile.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    dbContext.KocBodyProfiles.Add(bodyProfile);
                }

                bodyProfile.WeightKg = Input.WeightKg;
                bodyProfile.HeightCm = Input.HeightCm;
                bodyProfile.BustCm = Input.BustCm;
                bodyProfile.WaistCm = Input.WaistCm;
                bodyProfile.HipCm = Input.HipCm;
                bodyProfile.MaritalStatus = Input.MaritalStatus;
                bodyProfile.HasChildren = Input.HasChildren;
                bodyProfile.ChildrenCount = Input.HasChildren ? Input.Children.Count : 0;
                bodyProfile.UpdatedAtUtc = DateTime.UtcNow;

                var existingChildren = await dbContext.KocChildren.Where(x => x.KocBodyProfileId == bodyProfile.Id).ToListAsync(cancellationToken);
                dbContext.KocChildren.RemoveRange(existingChildren);
                if (Input.HasChildren)
                {
                    foreach (var child in Input.Children)
                    {
                        dbContext.KocChildren.Add(new KocChildEntity
                        {
                            Id = Guid.NewGuid(),
                            KocBodyProfileId = bodyProfile.Id,
                            AgeYears = child.Age,
                            Gender = child.Gender,
                            BirthYear = child.Age.HasValue ? DateTime.UtcNow.Year - child.Age.Value : null,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                var jobPreference = await dbContext.KocJobPreferences.FirstOrDefaultAsync(x => x.KocProfileId == kocProfile.Id, cancellationToken);
                if (jobPreference is null)
                {
                    jobPreference = new KocJobPreferenceEntity
                    {
                        Id = Guid.NewGuid(),
                        KocProfileId = kocProfile.Id,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    dbContext.KocJobPreferences.Add(jobPreference);
                }

                jobPreference.MinBudgetAmount = Input.ServicePriceVnd;
                jobPreference.UpdatedAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            SuccessMessage = "Lưu thông tin thành công.";
            return RedirectToPage(new { activeTab = section });
        }
        catch (DbUpdateException dbEx) when (TryMapUniqueViolationToModelState(dbEx))
        {
            await LoadLookupAsync(cancellationToken);
            ActiveTab = NormalizeSection(saveSection);
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "KOC profile update failed for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            await LoadLookupAsync(cancellationToken);
            ErrorMessage = BuildFriendlySaveErrorMessage(ex);
            ActiveTab = NormalizeSection(saveSection);
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
        var koc = await dbContext.KocProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null || koc is null)
        {
            return;
        }

        var body = await dbContext.KocBodyProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == koc.Id, cancellationToken);
        var children = body is null
            ? []
            : await dbContext.KocChildren.AsNoTracking().Where(x => x.KocBodyProfileId == body.Id).ToListAsync(cancellationToken);

        var socials = await dbContext.KocSocialAccounts.AsNoTracking().Where(x => x.KocProfileId == koc.Id).ToListAsync(cancellationToken);
        var selectedCategoryCodes = await (
            from pc in dbContext.KocProfileCategories.AsNoTracking()
            join c in dbContext.KocCategories.AsNoTracking() on pc.CategoryId equals c.Id
            where pc.KocProfileId == koc.Id
            select c.Code
        ).Distinct().ToListAsync(cancellationToken);

        var jobPref = await dbContext.KocJobPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == koc.Id, cancellationToken);

        Input = new InputModel
        {
            FullName = koc.DisplayName,
            Email = user.Email,
            Phone = user.Phone ?? string.Empty,
            Gender = user.Gender ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            StreetAddress = koc.StreetAddress ?? string.Empty,
            ProvinceId = koc.ProvinceId,
            WardId = koc.WardId,
            WeightKg = body?.WeightKg,
            HeightCm = body?.HeightCm,
            BustCm = body?.BustCm,
            WaistCm = body?.WaistCm,
            HipCm = body?.HipCm,
            MaritalStatus = body?.MaritalStatus,
            HasChildren = body?.HasChildren ?? false,
            ServicePriceVnd = jobPref?.MinBudgetAmount,
            Children = children.Select(x => new ChildInput { Age = x.AgeYears, Gender = x.Gender }).ToList(),
            Platforms = Platforms.Select(platform =>
            {
                var social = socials.FirstOrDefault(x => x.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
                return new SocialInput
                {
                    Platform = platform,
                    ChannelUrl = social?.ChannelUrl,
                    Followers = social?.Followers,
                    SelectedCategoryCodes = social is null ? [] : selectedCategoryCodes.ToList()
                };
            }).ToList()
        };

        if (Input.Platforms.Count == 0)
        {
            Input.Platforms = Platforms.Select(x => new SocialInput { Platform = x }).ToList();
        }

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

        var categories = await kocRegistrationService.GetCategoriesAsync(cancellationToken);
        CategoryOptions = categories.Select(x => new SelectListItem { Value = x.Code, Text = x.Name }).ToList();

        if (Input.Platforms.Count == 0)
        {
            Input.Platforms = Platforms.Select(x => new SocialInput { Platform = x }).ToList();
        }
    }

    private void NormalizeInput()
    {
        Input.FullName = (Input.FullName ?? string.Empty).Trim();
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Phone = (Input.Phone ?? string.Empty).Trim();
        Input.StreetAddress = (Input.StreetAddress ?? string.Empty).Trim();

        foreach (var platform in Input.Platforms)
        {
            platform.ChannelUrl = (platform.ChannelUrl ?? string.Empty).Trim();
            platform.SelectedCategoryCodes = platform.SelectedCategoryCodes
                .SelectMany(x => (x ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (!Input.HasChildren)
        {
            Input.Children.Clear();
        }
        else
        {
            Input.Children = Input.Children.Where(x => x.Age.HasValue || !string.IsNullOrWhiteSpace(x.Gender)).ToList();
        }
    }

    private static string NormalizeSection(string? saveSection)
    {
        var section = (saveSection ?? string.Empty).Trim().ToLowerInvariant();
        return section is "basic" or "social" or "personal" ? section : "basic";
    }

    private static string BuildFriendlySaveErrorMessage(Exception ex)
    {
        var root = ex.GetBaseException().Message;
        if (root.Contains("DateOfBirth", StringComparison.OrdinalIgnoreCase)
            || root.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
        {
            return "Không thể lưu vì CSDL chưa cập nhật cấu trúc mới (thiếu cột Ngày sinh). Vui lòng chạy script cập nhật database rồi thử lại.";
        }

        return "Đã xảy ra lỗi khi lưu hồ sơ KOC. Vui lòng thử lại, nếu vẫn lỗi hãy liên hệ kỹ thuật.";
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
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số nhà và tên đường.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
        public Guid? ProvinceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
        public Guid? WardId { get; set; }

        public decimal? WeightKg { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? BustCm { get; set; }
        public decimal? WaistCm { get; set; }
        public decimal? HipCm { get; set; }
        public string? MaritalStatus { get; set; }
        public bool HasChildren { get; set; }
        public decimal? ServicePriceVnd { get; set; }

        public List<ChildInput> Children { get; set; } = [];
        public List<SocialInput> Platforms { get; set; } = [];
    }

    public sealed class ChildInput
    {
        public int? Age { get; set; }
        public string? Gender { get; set; }
    }

    public sealed class SocialInput
    {
        public string Platform { get; set; } = string.Empty;
        public string? ChannelUrl { get; set; }
        public int? Followers { get; set; }
        public List<string> SelectedCategoryCodes { get; set; } = [];
    }
}
