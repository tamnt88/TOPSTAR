using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.SocialSync;

internal sealed class YouTubeMetricsClient(
    TopstarDbContext dbContext,
    ISocialTokenCipher tokenCipher,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<YouTubeMetricsClient> logger) : IYouTubeMetricsClient
{
    public async Task<YouTubeProfileMetrics?> FetchProfileMetricsAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await dbContext.SocialPlatformConnections.FirstOrDefaultAsync(x => x.Id == connectionId, cancellationToken);
        if (connection is null || !connection.Platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase))
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
        var res = await client.GetAsync("https://www.googleapis.com/youtube/v3/channels?part=statistics,snippet&mine=true", cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogWarning("YouTube API trả về {Status} cho connection {ConnectionId}", (int)res.StatusCode, connectionId);
            if ((int)res.StatusCode == 401)
            {
                var renewed = await RefreshAccessTokenAsync(connection, cancellationToken);
                if (!string.IsNullOrWhiteSpace(renewed))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed);
                    res = await client.GetAsync("https://www.googleapis.com/youtube/v3/channels?part=statistics,snippet&mine=true", cancellationToken);
                }
            }
        }

        if (!res.IsSuccessStatusCode)
        {
            return null;
        }

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var item = doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0
            ? items[0]
            : default;
        if (item.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        int? subscribers = null;
        int? totalViews = null;
        int? videosCount = null;
        string? handle = null;

        if (item.TryGetProperty("statistics", out var stats))
        {
            subscribers = TryParseInt(stats, "subscriberCount");
            totalViews = TryParseInt(stats, "viewCount");
            videosCount = TryParseInt(stats, "videoCount");
        }
        if (item.TryGetProperty("snippet", out var snippet))
        {
            handle = snippet.TryGetProperty("customUrl", out var c) ? c.GetString() : null;
        }

        int? avgViews = null;
        if (totalViews.HasValue && videosCount.GetValueOrDefault() > 0)
        {
            avgViews = totalViews.Value / videosCount.GetValueOrDefault();
        }

        connection.LastSyncedAtUtc = DateTime.UtcNow;
        connection.LastError = null;
        connection.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new YouTubeProfileMetrics(subscribers, avgViews, videosCount, handle);
    }

    private async Task<string?> RefreshAccessTokenAsync(Entities.SocialPlatformConnectionEntity connection, CancellationToken cancellationToken)
    {
        var refreshToken = tokenCipher.Unprotect(connection.RefreshTokenEncrypted ?? string.Empty);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            connection.LastError = $"Refresh token lỗi: {(int)response.StatusCode}";
            connection.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var accessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        connection.AccessTokenEncrypted = tokenCipher.Protect(accessToken);
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
