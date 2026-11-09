namespace Topstar.Infrastructure.Entities;

public sealed class SocialPlatformConnectionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string PlatformUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? ProfileUrl { get; set; }
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string? RefreshTokenEncrypted { get; set; }
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public string? Scopes { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public string? LastError { get; set; }
}

public sealed class SocialSyncJobEntity
{
    public Guid Id { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string RequestedByRole { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public Guid? KocProfileId { get; set; }
    public Guid? CampaignId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}

public sealed class SocialProfileMetricSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime SnapshotAtUtc { get; set; }
    public int? Followers { get; set; }
    public int? AvgViews { get; set; }
    public decimal? AvgEngagementRate { get; set; }
    public int? Reach { get; set; }
    public int? Impressions { get; set; }
    public long? Likes { get; set; }
    public long? Comments { get; set; }
    public long? Shares { get; set; }
    public int? PostsCount { get; set; }
    public string DataSource { get; set; } = "OAuthApi";
    public int ConfidenceScore { get; set; } = 95;
    public DateTime CreatedAtUtc { get; set; }
}
