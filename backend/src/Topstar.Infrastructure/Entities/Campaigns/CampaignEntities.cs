namespace Topstar.Infrastructure.Entities;

public sealed class CampaignEntity
{
    public Guid Id { get; set; }
    public Guid BrandProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public decimal BudgetAmount { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public bool RequiresBooking { get; set; }
    public bool RequiresShipment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? ReviewNote { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CampaignDeliverableEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string DeliverableType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ApplicationEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid KocProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PitchNote { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ShortlistEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public DateTime? SentToBrandAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ShortlistItemEntity
{
    public Guid ShortlistId { get; set; }
    public Guid ApplicationId { get; set; }
    public string SelectionSource { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ParticipationEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid KocProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ConfirmedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ScheduleEntity
{
    public Guid Id { get; set; }
    public Guid ParticipationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int RescheduleCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ShipmentEntity
{
    public Guid Id { get; set; }
    public Guid ParticipationId { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? StreetAddress { get; set; }
    public Guid? WardId { get; set; }
    public Guid? ProvinceId { get; set; }
    public string? AddressV2 { get; set; }
    public string? WardCodeV2 { get; set; }
    public bool IsNewAddress { get; set; }
    public string? TrackingCode { get; set; }
    public string? Carrier { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IssueNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SubmissionEntity
{
    public Guid Id { get; set; }
    public Guid ParticipationId { get; set; }
    public Guid DeliverableId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubmissionUrl { get; set; }
    public string? ReviewComment { get; set; }
    public int RevisionCount { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SubmissionPostEntity
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string PostUrl { get; set; } = string.Empty;
    public string? PostExternalId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PostMetricSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid SubmissionPostId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public long Views { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }
    public long Shares { get; set; }
    public long Saves { get; set; }
    public long Impressions { get; set; }
    public long Reach { get; set; }
    public long Clicks { get; set; }
    public decimal? EngagementRate { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CampaignKocDailyMetricEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid KocProfileId { get; set; }
    public DateOnly MetricDate { get; set; }
    public long Impressions { get; set; }
    public long Views { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }
    public long Shares { get; set; }
    public long Saves { get; set; }
    public long Clicks { get; set; }
    public long Conversions { get; set; }
    public decimal SpendAmount { get; set; }
    public decimal RevenueAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ReviewEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public Guid RevieweeUserId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
