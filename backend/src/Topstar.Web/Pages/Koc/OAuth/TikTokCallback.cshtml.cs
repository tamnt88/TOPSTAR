using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Koc.OAuth;

[Authorize(Roles = "Koc")]
public sealed class TikTokCallbackModel(
    TopstarDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ISocialTokenCipher tokenCipher) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    [TempData]
    public string? SyncMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            SyncMessage = $"TikTok trả về lỗi: {error}";
            return RedirectToPage("/Koc/Profile");
        }

        var expectedState = Request.Cookies["topstar.tiktok.state"];
        var codeVerifier = Request.Cookies["topstar.tiktok.verifier"];
        Response.Cookies.Delete("topstar.tiktok.state");
        Response.Cookies.Delete("topstar.tiktok.verifier");

        if (string.IsNullOrWhiteSpace(state) || !string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            SyncMessage = "Phiên OAuth TikTok không hợp lệ, vui lòng thử lại.";
            return RedirectToPage("/Koc/Profile");
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            SyncMessage = "Thiếu mã xác minh PKCE của TikTok. Vui lòng thử kết nối lại.";
            return RedirectToPage("/Koc/Profile");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            SyncMessage = "Không nhận được mã xác thực TikTok.";
            return RedirectToPage("/Koc/Profile");
        }

        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var clientKey = configuration["Authentication:TikTok:ClientKey"];
        var clientSecret = configuration["Authentication:TikTok:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientKey) || string.IsNullOrWhiteSpace(clientSecret))
        {
            SyncMessage = "Thiếu cấu hình TikTok OAuth.";
            return RedirectToPage("/Koc/Profile");
        }

        var callbackUrl = ResolveTikTokRedirectUri();
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            SyncMessage = "Thiếu cấu hình redirect TikTok. Vui lòng cấu hình Authentication:TikTok:RedirectUri.";
            return RedirectToPage("/Koc/Profile");
        }

        var client = httpClientFactory.CreateClient();
        var tokenReq = new Dictionary<string, string>
        {
            ["client_key"] = clientKey,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = callbackUrl,
            ["code_verifier"] = codeVerifier
        };

        var tokenRes = await client.PostAsync(
            "https://open.tiktokapis.com/v2/oauth/token/",
            new FormUrlEncodedContent(tokenReq),
            cancellationToken);

        if (!tokenRes.IsSuccessStatusCode)
        {
            SyncMessage = $"Không lấy được token TikTok ({(int)tokenRes.StatusCode}).";
            return RedirectToPage("/Koc/Profile");
        }

        using var tokenStream = await tokenRes.Content.ReadAsStreamAsync(cancellationToken);
        using var tokenDoc = await JsonDocument.ParseAsync(tokenStream, cancellationToken: cancellationToken);
        var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var refreshToken = tokenDoc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var sec) ? sec : 3600;
        var openId = tokenDoc.RootElement.TryGetProperty("open_id", out var oid) ? oid.GetString() : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            SyncMessage = "TikTok không trả về access token.";
            return RedirectToPage("/Koc/Profile");
        }

        string username = "tiktok-user";
        string? profileUrl = null;
        try
        {
            using var infoReq = new HttpRequestMessage(
                HttpMethod.Get,
                "https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name,username,profile_web_link,profile_deep_link");
            infoReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var infoRes = await client.SendAsync(infoReq, cancellationToken);
            if (infoRes.IsSuccessStatusCode)
            {
                using var infoStream = await infoRes.Content.ReadAsStreamAsync(cancellationToken);
                using var infoDoc = await JsonDocument.ParseAsync(infoStream, cancellationToken: cancellationToken);
                if (infoDoc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("user", out var user))
                {
                    var dn = user.TryGetProperty("display_name", out var d) ? d.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(dn))
                    {
                        username = dn;
                    }

                    var un = user.TryGetProperty("username", out var uv) ? uv.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(un))
                    {
                        username = un;
                    }

                    profileUrl = user.TryGetProperty("profile_web_link", out var pw) ? pw.GetString() : null;
                    if (string.IsNullOrWhiteSpace(profileUrl))
                    {
                        profileUrl = user.TryGetProperty("profile_deep_link", out var pd) ? pd.GetString() : null;
                    }

                    if (string.IsNullOrWhiteSpace(openId))
                    {
                        openId = user.TryGetProperty("open_id", out var x) ? x.GetString() : openId;
                    }
                }
            }
        }
        catch
        {
            // best effort
        }

        var row = await dbContext.SocialPlatformConnections
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Platform == "TikTok", cancellationToken);
        if (row is null)
        {
            row = new SocialPlatformConnectionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Platform = "TikTok",
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.SocialPlatformConnections.Add(row);
        }

        row.PlatformUserId = openId ?? row.PlatformUserId ?? Guid.NewGuid().ToString("N");
        row.Username = username;
        row.ProfileUrl = !string.IsNullOrWhiteSpace(profileUrl) ? profileUrl : $"https://www.tiktok.com/@{username}";
        row.AccessTokenEncrypted = tokenCipher.Protect(accessToken);
        row.RefreshTokenEncrypted = string.IsNullOrWhiteSpace(refreshToken) ? row.RefreshTokenEncrypted : tokenCipher.Protect(refreshToken);
        row.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
        row.Status = "Active";
        row.Scopes = "user.info.profile,user.info.stats,video.list";
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.LastError = null;

        var kocProfile = await dbContext.KocProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (kocProfile is not null)
        {
            var social = await dbContext.KocSocialAccounts
                .FirstOrDefaultAsync(x => x.KocProfileId == kocProfile.Id && x.Platform == "TikTok", cancellationToken);
            if (social is not null)
            {
                social.ChannelUrl = row.ProfileUrl;
                social.Handle = username;
                social.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        SyncMessage = "Đã kết nối TikTok thành công.";
        return RedirectToPage("/Koc/Profile");
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
}
