using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.SocialSync;

internal sealed class TikTokMetricsClient(
    TopstarDbContext dbContext,
    ISocialTokenCipher tokenCipher,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TikTokMetricsClient> logger) : ITikTokMetricsClient
{
    public async Task<TikTokProfileMetrics?> FetchProfileMetricsAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await dbContext.SocialPlatformConnections.FirstOrDefaultAsync(x => x.Id == connectionId, cancellationToken);
        if (connection is null || !connection.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var accessToken = tokenCipher.Unprotect(connection.AccessTokenEncrypted);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        if (connection.AccessTokenExpiresAtUtc.HasValue && connection.AccessTokenExpiresAtUtc.Value <= DateTime.UtcNow.AddMinutes(-1))
        {
            accessToken = await RefreshAccessTokenAsync(connection, cancellationToken) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var url = "https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name,follower_count,video_count";
        var res = await client.GetAsync(url, cancellationToken);
        if (!res.IsSuccessStatusCode && (int)res.StatusCode == 401)
        {
            var renewed = await RefreshAccessTokenAsync(connection, cancellationToken);
            if (!string.IsNullOrWhiteSpace(renewed))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed);
                res = await client.GetAsync(url, cancellationToken);
            }
        }

        if (!res.IsSuccessStatusCode)
        {
            logger.LogWarning("TikTok API trả về {Status} cho connection {ConnectionId}", (int)res.StatusCode, connectionId);
            connection.LastError = $"TikTok API lỗi {(int)res.StatusCode}";
            connection.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var user = doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("user", out var u) ? u : default;
        if (user.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var followers = TryParseInt(user, "follower_count");
        var videos = TryParseInt(user, "video_count");
        // TikTok /user/info does not return total video views. Followers/video count
        // is not an average-view metric, so keep it unknown instead of fabricating data.
        int? avgViews = null;
        var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;

        connection.LastSyncedAtUtc = DateTime.UtcNow;
        connection.LastError = null;
        connection.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TikTokProfileMetrics(followers, avgViews, videos, displayName);
    }

    private async Task<string?> RefreshAccessTokenAsync(Entities.SocialPlatformConnectionEntity connection, CancellationToken cancellationToken)
    {
        var refreshToken = tokenCipher.Unprotect(connection.RefreshTokenEncrypted ?? string.Empty);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var clientKey = configuration["Authentication:TikTok:ClientKey"];
        var clientSecret = configuration["Authentication:TikTok:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientKey) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["client_key"] = clientKey,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        var response = await client.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            connection.LastError = $"Refresh token TikTok lỗi: {(int)response.StatusCode}";
            connection.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var accessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var seconds) ? seconds : 3600;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var newRefresh = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        connection.AccessTokenEncrypted = tokenCipher.Protect(accessToken);
        if (!string.IsNullOrWhiteSpace(newRefresh))
        {
            connection.RefreshTokenEncrypted = tokenCipher.Protect(newRefresh);
        }
        connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
        connection.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return accessToken;
    }

    private static int? TryParseInt(JsonElement obj, string key)
    {
        if (!obj.TryGetProperty(key, out var val))
        {
            return null;
        }

        if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var i))
        {
            return i;
        }

        if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var j))
        {
            return j;
        }

        return null;
    }
}
