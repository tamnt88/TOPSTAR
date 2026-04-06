using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    TopstarDbContext dbContext) : KocGuardedPageModel(dashboardDataService)
{
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
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

        var selectedCategoryCount = Input.Platforms
            .SelectMany(x => x.SelectedCategoryCodes)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (selectedCategoryCount < 3)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất 3 danh mục ngành hàng.");
        }

        await LoadLookupAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);
        var kocProfile = await dbContext.KocProfiles.FirstOrDefaultAsync(x => x.UserId == parsedUserId, cancellationToken);
        if (user is null || kocProfile is null)
        {
            ErrorMessage = "Không tìm thấy hồ sơ KOC.";
            return Page();
        }

        user.Email = Input.Email.Trim();
        user.Phone = Input.Phone.Trim();
        user.Gender = Input.Gender;
        user.UpdatedAtUtc = DateTime.UtcNow;

        kocProfile.DisplayName = Input.FullName.Trim();
        kocProfile.StreetAddress = Input.StreetAddress.Trim();
        kocProfile.ProvinceId = Input.ProvinceId;
        kocProfile.WardId = Input.WardId;
        kocProfile.Status = "Submitted";
        kocProfile.UpdatedAtUtc = DateTime.UtcNow;

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

        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Koc/Dashboard");
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
                    SelectedCategoryCodes = selectedCategoryCodes.ToList()
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

    public sealed class InputModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        public Guid? ProvinceId { get; set; }

        [Required]
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
