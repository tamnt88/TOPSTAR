using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using Topstar.Web.Services.Profiles;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class ProfileModel(
    IProfileAccessService profileAccessService,
    TopstarDbContext dbContext,
    ISocialSyncOrchestrator socialSyncOrchestrator,
    IKocProfileViewService profileViewService,
    IConfiguration configuration) : KocGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public bool BrandPreview { get; set; }
    [TempData]
    public string? SyncMessage { get; set; }

    public KocProfileViewData Data { get; private set; } = new();
    public ProfileCompletionView Completion { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: true);
        if (redirect is not null)
        {
            return redirect;
        }

        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var access = await ProfileAccessService.CheckKocAsync(userId, cancellationToken);
        Completion = BuildCompletion(access);
        Data = await profileViewService.GetAsync(userId, cancellationToken) ?? new KocProfileViewData();

        return Page();
    }

    public async Task<IActionResult> OnPostSyncNowAsync(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var result = await socialSyncOrchestrator.RequestKocSelfSyncAsync(userId, cancellationToken);
        SyncMessage = result.Message;
        return RedirectToPage();
    }

    public IActionResult OnGetConnectYouTube()
    {
        var redirectUrl = Url.Page("/Koc/OAuth/GoogleYoutubeCallback");
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        return Challenge(properties, "GoogleYouTube");
    }

    public IActionResult OnGetConnectPlatform(string platform)
    {
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            return OnGetConnectYouTube();
        }

        if (string.Equals(platform, "TikTok", StringComparison.OrdinalIgnoreCase))
        {
            var clientKey = configuration["Authentication:TikTok:ClientKey"];
            if (string.IsNullOrWhiteSpace(clientKey))
            {
                SyncMessage = "Thiếu cấu hình TikTok ClientKey.";
                return RedirectToPage();
            }

            var callbackUrl = ResolveTikTokRedirectUri();
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                SyncMessage = "Thiếu cấu hình redirect TikTok. Vui lòng cấu hình Authentication:TikTok:RedirectUri.";
                return RedirectToPage();
            }

            if (callbackUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                SyncMessage = $"TikTok không hỗ trợ localhost cho redirect_uri. URI hiện tại: {callbackUrl}";
                return RedirectToPage();
            }

            var state = Guid.NewGuid().ToString("N");
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            Response.Cookies.Append("topstar.tiktok.state", state, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            Response.Cookies.Append("topstar.tiktok.verifier", codeVerifier, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });

            var authUrl = "https://www.tiktok.com/v2/auth/authorize/"
                + $"?client_key={Uri.EscapeDataString(clientKey)}"
                + "&response_type=code"
                + "&scope=user.info.profile,user.info.stats,video.list"
                + $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}"
                + $"&state={Uri.EscapeDataString(state)}"
                + $"&code_challenge={Uri.EscapeDataString(codeChallenge)}"
                + "&code_challenge_method=S256";
            return Redirect(authUrl);
        }

        SyncMessage = $"Nền tảng {platform} chưa hỗ trợ OAuth ở phiên bản này.";
        return RedirectToPage();
    }

    
    private string? ResolveTikTokRedirectUri()
    {
        if (!Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return Url.Page("/Koc/OAuth/TikTokCallback", null, null, Request.Scheme);
        }

        var env = Environment.GetEnvironmentVariable("TOPSTAR_TIKTOK_REDIRECT_URI");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        var configured = configuration["Authentication:TikTok:RedirectUri"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return Url.Page("/Koc/OAuth/TikTokCallback", null, null, Request.Scheme);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public async Task<IActionResult> OnPostSyncPlatformAsync(string platform, CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var result = await socialSyncOrchestrator.RequestKocSelfSyncAsync(userId, cancellationToken);
        SyncMessage = result.Success
            ? $"Đã gửi yêu cầu đồng bộ cho nền tảng {platform}."
            : result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisconnectPlatformAsync(string platform, CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            SyncMessage = "Thiếu thông tin nền tảng cần ngắt kết nối.";
            return RedirectToPage();
        }

        var row = await dbContext.SocialPlatformConnections
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Platform == platform, cancellationToken);
        if (row is null)
        {
            SyncMessage = $"Không tìm thấy kết nối {platform}.";
            return RedirectToPage();
        }

        row.Status = "Disconnected";
        row.AccessTokenEncrypted = string.Empty;
        row.RefreshTokenEncrypted = string.Empty;
        row.AccessTokenExpiresAtUtc = null;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.LastError = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        SyncMessage = $"Đã ngắt kết nối {platform}.";
        return RedirectToPage();
    }

    public sealed class ProfileCompletionView
    {
        public int Percent { get; init; }
        public bool IsComplete { get; init; }
        public bool IsApproved { get; init; }
        public string StatusLabel { get; init; } = "Đang cập nhật";
        public IReadOnlyList<ProfileCompletionItem> MissingItems { get; init; } = [];
    }

    public sealed record ProfileCompletionItem(string Label, string Detail, string Tab, string Icon);

    private static ProfileCompletionView BuildCompletion(ProfileAccessResult access)
    {
        const int requiredFieldCount = 13;
        var fieldMap = new Dictionary<string, ProfileCompletionItem>(StringComparer.OrdinalIgnoreCase)
        {
            ["KocProfile"] = new("Khởi tạo hồ sơ KOC", "Hồ sơ KOC chưa tồn tại.", "basic", "fa-id-card"),
            ["Email"] = new("Email", "Bổ sung địa chỉ email để nhận thông báo.", "basic", "fa-envelope"),
            ["Phone"] = new("Số điện thoại", "Bổ sung số điện thoại liên hệ.", "basic", "fa-phone"),
            ["Gender"] = new("Giới tính", "Chọn giới tính trong thông tin cơ bản.", "basic", "fa-venus-mars"),
            ["DateOfBirth"] = new("Ngày sinh", "Bổ sung ngày sinh chính xác.", "basic", "fa-cake-candles"),
            ["DisplayName"] = new("Tên hiển thị", "Nhập tên KOC Brand sẽ nhìn thấy.", "basic", "fa-signature"),
            ["StreetAddress"] = new("Địa chỉ", "Bổ sung địa chỉ liên hệ.", "basic", "fa-location-dot"),
            ["ProvinceId"] = new("Tỉnh/thành phố", "Chọn tỉnh hoặc thành phố đang hoạt động.", "basic", "fa-map"),
            ["WardId"] = new("Phường/xã", "Chọn phường hoặc xã tương ứng.", "basic", "fa-map-pin"),
            ["BodyProfile"] = new("Thông tin cá nhân", "Bổ sung chiều cao, cân nặng và tình trạng hôn nhân.", "personal", "fa-person"),
            ["WeightKg"] = new("Cân nặng", "Nhập cân nặng hiện tại.", "personal", "fa-weight-scale"),
            ["HeightCm"] = new("Chiều cao", "Nhập chiều cao hiện tại.", "personal", "fa-ruler-vertical"),
            ["MaritalStatus"] = new("Tình trạng hôn nhân", "Chọn tình trạng hôn nhân.", "personal", "fa-people-roof"),
            ["SocialAccount"] = new("Kênh mạng xã hội", "Thêm ít nhất một kênh có đường dẫn hợp lệ.", "social", "fa-share-nodes"),
            ["Categories"] = new("Lĩnh vực nội dung", "Chọn ít nhất 3 lĩnh vực nội dung phù hợp.", "social", "fa-tags")
        };

        var missingItems = access.MissingFields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(field => fieldMap.TryGetValue(field, out var item)
                ? item
                : new ProfileCompletionItem(field, "Bổ sung thông tin bắt buộc này.", "basic", "fa-circle-exclamation"))
            .ToList();
        var missingRequirementCount = access.MissingFields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Sum(field => field.Equals("KocProfile", StringComparison.OrdinalIgnoreCase)
                ? requiredFieldCount
                : field.Equals("BodyProfile", StringComparison.OrdinalIgnoreCase) ? 3 : 1);
        var percent = access.Exists
            ? Math.Clamp((int)Math.Round((requiredFieldCount - Math.Min(requiredFieldCount, missingRequirementCount)) * 100m / requiredFieldCount), 0, 100)
            : 0;
        var statusLabel = access.Status.ToLowerInvariant() switch
        {
            "approved" => "Đã duyệt",
            "pendingreview" => "Đang chờ duyệt",
            "needupdate" => "Cần bổ sung",
            "rejected" => "Đã từ chối",
            "draft" => "Bản nháp",
            _ => "Đang cập nhật"
        };

        return new ProfileCompletionView
        {
            Percent = percent,
            IsComplete = access.IsComplete,
            IsApproved = access.IsApproved,
            StatusLabel = statusLabel,
            MissingItems = missingItems
        };
    }

}

