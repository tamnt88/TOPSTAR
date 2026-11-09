using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Koc.OAuth;

[Authorize(Roles = "Koc")]
public sealed class GoogleYoutubeCallbackModel(
    TopstarDbContext dbContext,
    ISocialTokenCipher tokenCipher,
    IHttpClientFactory httpClientFactory) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    [TempData]
    public string? SyncMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var authResult = await HttpContext.AuthenticateAsync("External");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            SyncMessage = "Kết nối YouTube thất bại. Vui lòng thử lại.";
            return RedirectToPage("/Koc/Profile");
        }

        var accessToken = authResult.Properties?.GetTokenValue("access_token");
        var refreshToken = authResult.Properties?.GetTokenValue("refresh_token");
        var expiresAtRaw = authResult.Properties?.GetTokenValue("expires_at");
        DateTime? expiresAt = null;
        if (DateTime.TryParse(expiresAtRaw, out var exp))
        {
            expiresAt = exp.ToUniversalTime();
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            SyncMessage = "Không nhận được access token YouTube.";
            return RedirectToPage("/Koc/Profile");
        }

        var platformUserId = authResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? authResult.Principal.FindFirst("sub")?.Value
            ?? Guid.NewGuid().ToString("N");
        var username = authResult.Principal.Identity?.Name
            ?? authResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "youtube-user";

        var row = await dbContext.SocialPlatformConnections
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Platform == "YouTube", cancellationToken);
        if (row is null)
        {
            row = new SocialPlatformConnectionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Platform = "YouTube",
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.SocialPlatformConnections.Add(row);
        }

        string? profileUrl = null;
        try
        {
            var client = httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var res = await client.SendAsync(req, cancellationToken);
            if (res.IsSuccessStatusCode)
            {
                using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("items", out var items)
                    && items.ValueKind == System.Text.Json.JsonValueKind.Array
                    && items.GetArrayLength() > 0)
                {
                    var first = items[0];
                    if (first.TryGetProperty("id", out var idNode))
                    {
                        var channelId = idNode.GetString();
                        if (!string.IsNullOrWhiteSpace(channelId))
                        {
                            profileUrl = $"https://www.youtube.com/channel/{channelId}";
                        }
                    }

                    if (first.TryGetProperty("snippet", out var snippet))
                    {
                        var title = snippet.TryGetProperty("title", out var titleNode) ? titleNode.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            username = title;
                        }
                    }
                }
            }
        }
        catch
        {
            // best effort
        }

        row.PlatformUserId = platformUserId;
        row.Username = username;
        row.ProfileUrl = !string.IsNullOrWhiteSpace(profileUrl) ? profileUrl : $"https://www.youtube.com/@{username}";
        row.AccessTokenEncrypted = tokenCipher.Protect(accessToken);
        row.RefreshTokenEncrypted = string.IsNullOrWhiteSpace(refreshToken) ? row.RefreshTokenEncrypted : tokenCipher.Protect(refreshToken);
        row.AccessTokenExpiresAtUtc = expiresAt;
        row.Status = "Active";
        row.Scopes = "youtube.readonly";
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.LastError = null;

        var kocProfile = await dbContext.KocProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (kocProfile is not null)
        {
            var social = await dbContext.KocSocialAccounts
                .FirstOrDefaultAsync(x => x.KocProfileId == kocProfile.Id && x.Platform == "YouTube", cancellationToken);
            if (social is not null)
            {
                social.ChannelUrl = row.ProfileUrl;
                social.Handle = username;
                social.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await HttpContext.SignOutAsync("External");

        SyncMessage = "Đã kết nối YouTube thành công.";
        return RedirectToPage("/Koc/Profile");
    }
}
