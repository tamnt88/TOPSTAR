using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Storage;
using Topstar.Web.Services.Profiles;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class EditProfileModel(
    IProfileAccessService profileAccessService,
    IKocRegistrationService kocRegistrationService,
    TopstarDbContext dbContext,
    IFileStorageService fileStorageService,
    IKocProfileSectionService profileSectionService,
    ILogger<EditProfileModel> logger) : KocGuardedPageModel(profileAccessService)
{
    [TempData]
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "basic";

    [BindProperty]
    public InputModel Input { get; set; } = new();
    [BindProperty]
    public IFormFile? AvatarFile { get; set; }
    [BindProperty]
    public IFormFile? ProfileDocumentFile { get; set; }
    [BindProperty]
    public List<IFormFile>? GalleryFiles { get; set; }
    [BindProperty]
    public List<IFormFile>? VideoFiles { get; set; }
    [BindProperty]
    public string? GalleryOrderCsv { get; set; }
    [BindProperty]
    public string? GalleryDeleteCsv { get; set; }
    [BindProperty]
    public string? VideoOrderCsv { get; set; }
    [BindProperty]
    public string? VideoDeleteCsv { get; set; }
    [BindProperty]
    public long ProfileVersionTicks { get; set; }

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
        var createdFiles = new List<string>();
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return RedirectToPage("/Public/Login");
            }

            var submittedSection = (saveSection ?? string.Empty).Trim().ToLowerInvariant();
            if (submittedSection is not ("basic" or "social" or "personal" or "gallery" or "video"))
            {
                await LoadLookupAsync(cancellationToken);
                await LoadCurrentDataAsync(parsedUserId, cancellationToken);
                ActiveTab = "basic";
                ErrorMessage = "Không xác định được nhóm thông tin cần lưu. Vui lòng tải lại trang và thử lại.";
                return Page();
            }
            var section = submittedSection;

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

            if (section == "basic")
            {
                ValidateUpload(AvatarFile, "AvatarFile", [".jpg", ".jpeg", ".png", ".webp"], 5 * 1024 * 1024, "Ảnh đại diện");
                ValidateUpload(ProfileDocumentFile, "ProfileDocumentFile", [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp"], 10 * 1024 * 1024, "Hồ sơ năng lực");
            }
            else if (section == "gallery")
            {
                foreach (var file in GalleryFiles ?? [])
                    ValidateUpload(file, "GalleryFiles", [".jpg", ".jpeg", ".png", ".webp"], 8 * 1024 * 1024, "Ảnh thư viện");
            }
            else if (section == "video")
            {
                foreach (var file in VideoFiles ?? [])
                    ValidateUpload(file, "VideoFiles", [".mp4", ".webm", ".mov"], 100 * 1024 * 1024, "Video");
            }
            else if (section == "personal")
            {
                ValidateRange(Input.HeightCm, 80, 250, "Input.HeightCm", "Chiều cao");
                ValidateRange(Input.WeightKg, 20, 300, "Input.WeightKg", "Cân nặng");
                if (Input.ServicePriceVnd is < 0)
                    ModelState.AddModelError("Input.ServicePriceVnd", "Mức ngân sách mong muốn không được âm.");
                for (var i = 0; i < Input.Children.Count; i++)
                    if (Input.Children[i].Age is < 0 or > 17)
                        ModelState.AddModelError($"Input.Children[{i}].Age", "Tuổi của con phải nằm trong khoảng 0–17.");
            }

            await LoadLookupAsync(cancellationToken);
            if (!ModelState.IsValid)
            {
                ActiveTab = section;
                return Page();
            }

            var kocProfileId = await profileSectionService.GetKocProfileIdAsync(parsedUserId, cancellationToken);
            if (!kocProfileId.HasValue)
            {
                ErrorMessage = "Không tìm thấy hồ sơ KOC.";
                return Page();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var expectedUpdatedAt = ProfileVersionTicks > 0 ? new DateTime(ProfileVersionTicks, DateTimeKind.Utc) : (DateTime?)null;
            var versionUpdated = await dbContext.KocProfiles
                .Where(x => x.Id == kocProfileId.Value && x.UpdatedAtUtc == expectedUpdatedAt)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
            if (versionUpdated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                await CleanupCreatedFilesAsync(createdFiles);
                await LoadCurrentDataAsync(parsedUserId, cancellationToken);
                ErrorMessage = "Hồ sơ đã được cập nhật ở một cửa sổ hoặc phiên khác. Dữ liệu mới nhất đã được tải lại; vui lòng kiểm tra rồi lưu lại.";
                ActiveTab = section;
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
                string? avatarUrl = null;
                string? documentUrl = null;
                if (AvatarFile is not null && AvatarFile.Length > 0)
                {
                    var storedUrl = await fileStorageService.SaveKocProfileUploadAsync(AvatarFile, "avatar", cancellationToken);
                    if (storedUrl is null) throw new InvalidOperationException("Không thể lưu ảnh đại diện hợp lệ.");
                    createdFiles.Add(storedUrl);
                    avatarUrl = storedUrl;
                }
                if (ProfileDocumentFile is not null && ProfileDocumentFile.Length > 0)
                {
                    var ext = Path.GetExtension(ProfileDocumentFile.FileName)?.ToLowerInvariant() ?? string.Empty;
                    var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp"
                    };
                    if (!allowedExt.Contains(ext))
                    {
                        await CleanupCreatedFilesAsync(createdFiles);
                        ModelState.AddModelError("ProfileDocumentFile", "Định dạng file không hợp lệ. Vui lòng chọn PDF, Word hoặc hình ảnh.");
                        ActiveTab = section;
                        return Page();
                    }

                    var storedUrl = await fileStorageService.SaveKocProfileUploadAsync(ProfileDocumentFile, "document", cancellationToken);
                    if (storedUrl is null) throw new InvalidOperationException("Không thể lưu hồ sơ năng lực hợp lệ.");
                    createdFiles.Add(storedUrl);
                    documentUrl = storedUrl;
                }
                try
                {
                    await profileSectionService.SaveBasicAsync(parsedUserId, new KocBasicSectionInput(
                        Input.FullName, Input.Email, Input.Phone, Input.Gender, Input.DateOfBirth,
                        Input.StreetAddress, Input.ProvinceId, Input.WardId, avatarUrl, documentUrl), cancellationToken);
                    AddProfileAudit(parsedUserId, kocProfileId.Value, "UpdateIdentityProfile", new
                    {
                        Section = "basic",
                        Fields = new[] { "FullName", "Email", "Phone", "Gender", "DateOfBirth", "StreetAddress", "ProvinceId", "WardId" },
                        HasAvatarUpload = avatarUrl is not null,
                        HasProfileDocumentUpload = documentUrl is not null
                    });
                }
                catch (SqlException ex) when (ex.Message.Contains("KocMediaAssets", StringComparison.OrdinalIgnoreCase))
                {
                    await CleanupCreatedFilesAsync(createdFiles);
                    ModelState.AddModelError("ProfileDocumentFile", "CSDL chưa cập nhật bảng thư viện media KOC. Vui lòng chạy script cập nhật database.");
                    ActiveTab = section;
                    return Page();
                }
            }
            else if (section == "social")
            {
                var socialItems = Input.Platforms
                    .Where(x => !string.IsNullOrWhiteSpace(x.ChannelUrl))
                    .Select(x => new KocSocialSectionItem(x.Platform, x.ChannelUrl!.Trim(), x.Followers ?? 0, x.SelectedCategoryCodes))
                    .ToList();
                await profileSectionService.SaveSocialAsync(kocProfileId.Value, socialItems, cancellationToken);
                AddProfileAudit(parsedUserId, kocProfileId.Value, "ReplaceSocialAccounts", new
                {
                    Section = "social",
                    AccountCount = socialItems.Count,
                    Platforms = socialItems.Select(x => x.Platform).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    CategoryLinkCount = socialItems.Sum(x => x.CategoryCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                });
            }
            else if (section is "gallery" or "video")
            {
                var isGallery = section == "gallery";
                var files = isGallery ? GalleryFiles ?? [] : VideoFiles ?? [];
                var newUrls = new List<string>();
                foreach (var file in files.Where(x => x is not null && x.Length > 0))
                {
                    var storedUrl = await fileStorageService.SaveKocProfileUploadAsync(file, isGallery ? "gallery" : "video", cancellationToken);
                    if (storedUrl is null) throw new InvalidOperationException(isGallery ? "Không thể lưu ảnh thư viện hợp lệ." : "Không thể lưu video hợp lệ.");
                    createdFiles.Add(storedUrl);
                    newUrls.Add(storedUrl);
                }
                try
                {
                    await profileSectionService.SaveMediaAsync(
                        kocProfileId.Value,
                        isGallery ? "GalleryImage" : "GalleryVideo",
                        ParseGuidCsv(isGallery ? GalleryOrderCsv : VideoOrderCsv),
                        ParseGuidCsv(isGallery ? GalleryDeleteCsv : VideoDeleteCsv),
                        newUrls,
                        cancellationToken);
                }
                catch (SqlException ex) when (ex.Message.Contains("KocMediaAssets", StringComparison.OrdinalIgnoreCase))
                {
                    await CleanupCreatedFilesAsync(createdFiles);
                    ModelState.AddModelError(isGallery ? "GalleryFiles" : "VideoFiles", "CSDL chưa cập nhật bảng thư viện media KOC. Vui lòng chạy script cập nhật database.");
                    ActiveTab = section;
                    return Page();
                }
            }
            else
            {
                var personalInput = new KocPersonalSectionInput(
                    Input.WeightKg, Input.HeightCm, Input.BustCm, Input.WaistCm, Input.HipCm,
                    Input.MaritalStatus, Input.HasChildren, Input.ServicePriceVnd,
                    Input.Children.Select(x => new KocChildSectionItem(x.Age, x.Gender)).ToList());
                await profileSectionService.SavePersonalAsync(kocProfileId.Value, personalInput, cancellationToken);
                AddProfileAudit(parsedUserId, kocProfileId.Value, "UpdatePersonalProfile", new
                {
                    Section = "personal",
                    Fields = new[] { "WeightKg", "HeightCm", "MaritalStatus", "HasChildren", "ServicePriceVnd", "Children" }
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var savedAt = Topstar.Web.Features.Common.PortalFormat.ToVietnamTime(DateTime.UtcNow);
            SuccessMessage = $"Đã lưu thông tin lúc {savedAt:HH:mm, dd/MM/yyyy}.";
            return RedirectToPage(new { activeTab = section });
        }
        catch (DbUpdateException dbEx) when (TryMapUniqueViolationToModelState(dbEx))
        {
            await CleanupCreatedFilesAsync(createdFiles);
            await LoadLookupAsync(cancellationToken);
            ActiveTab = NormalizeSection(saveSection);
            return Page();
        }
        catch (Exception ex)
        {
            await CleanupCreatedFilesAsync(createdFiles);
            logger.LogError(ex, "KOC profile update failed for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            await LoadLookupAsync(cancellationToken);
            ErrorMessage = BuildFriendlySaveErrorMessage(ex);
            ActiveTab = NormalizeSection(saveSection);
            return Page();
        }
    }

    private void AddProfileAudit(Guid actorUserId, Guid profileId, string actionName, object safeMetadata)
    {
        dbContext.AuditLogs.Add(new AuditLogEntity
        {
            ActorUserId = actorUserId,
            EntityName = "KocProfile",
            EntityId = profileId.ToString(),
            ActionName = actionName,
            PayloadJson = JsonSerializer.Serialize(safeMetadata),
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task CleanupCreatedFilesAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await fileStorageService.DeleteKocProfileUploadAsync(path, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogWarning(cleanupException, "Could not remove orphaned KOC profile upload {Path}", path);
            }
        }
    }

    private void ValidateUpload(IFormFile? file, string fieldName, IReadOnlyCollection<string> allowedExtensions, long maximumBytes, string label)
    {
        if (file is null || file.Length == 0) return;
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(fieldName, $"{label} không đúng định dạng hỗ trợ.");
        if (file.Length > maximumBytes)
            ModelState.AddModelError(fieldName, $"{label} vượt quá dung lượng tối đa {maximumBytes / 1024 / 1024} MB.");
    }

    private void ValidateRange(decimal? value, decimal minimum, decimal maximum, string fieldName, string label)
    {
        if (value.HasValue && (value.Value < minimum || value.Value > maximum))
            ModelState.AddModelError(fieldName, $"{label} phải nằm trong khoảng {minimum:0.#}–{maximum:0.#}.");
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
        var snapshot = await profileSectionService.GetEditSnapshotAsync(userId, Platforms, cancellationToken);
        if (snapshot is null) return;
        Input = new InputModel
        {
            FullName = snapshot.FullName, Email = snapshot.Email, Phone = snapshot.Phone, Gender = snapshot.Gender,
            DateOfBirth = snapshot.DateOfBirth, CurrentAvatarUrl = snapshot.AvatarUrl, StreetAddress = snapshot.StreetAddress,
            ProvinceId = snapshot.ProvinceId, WardId = snapshot.WardId, WeightKg = snapshot.WeightKg, HeightCm = snapshot.HeightCm,
            BustCm = snapshot.BustCm, WaistCm = snapshot.WaistCm, HipCm = snapshot.HipCm, MaritalStatus = snapshot.MaritalStatus,
            HasChildren = snapshot.HasChildren, ServicePriceVnd = snapshot.ServicePriceVnd,
            Children = snapshot.Children.Select(x => new ChildInput { Age = x.Age, Gender = x.Gender }).ToList(),
            Platforms = snapshot.Socials.Select(x => new SocialInput { Platform = x.Platform, ChannelUrl = x.ChannelUrl, Followers = x.Followers, SelectedCategoryCodes = x.CategoryCodes.ToList() }).ToList(),
            ExistingGalleryItems = snapshot.Gallery.Select(x => new MediaItem { Id = x.Id, FileUrl = x.FileUrl, SortOrder = x.SortOrder }).ToList(),
            ExistingVideoItems = snapshot.Videos.Select(x => new MediaItem { Id = x.Id, FileUrl = x.FileUrl, SortOrder = x.SortOrder }).ToList(),
            CurrentProfileDocumentUrl = snapshot.ProfileDocumentUrl
        };
        ProfileVersionTicks = snapshot.ProfileUpdatedAtUtc?.Ticks ?? 0;
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
        return section is "basic" or "social" or "personal" or "gallery" or "video" ? section : "basic";
    }

    private static List<Guid> ParseGuidCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToList();
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
        public string? CurrentAvatarUrl { get; set; }
        public string? CurrentProfileDocumentUrl { get; set; }
        public List<MediaItem> ExistingGalleryItems { get; set; } = [];
        public List<MediaItem> ExistingVideoItems { get; set; } = [];

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

    public sealed class MediaItem
    {
        public Guid Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
