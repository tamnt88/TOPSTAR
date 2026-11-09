namespace Topstar.Infrastructure.Entities;

public sealed class CampaignEntity
{
    public bool CheckoutAtCreation { get; set; }
    public bool BrandSelectionSnapshot { get; set; }
    public int SelectionQuotaSnapshot { get; set; }
    public decimal KocPlatformFeeRateSnapshot { get; set; }

    public Guid Id { get; set; }
    public Guid BrandProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Objective { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? AvatarUrl { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(500)] public string? BannerUrl { get; set; }
    public decimal BudgetAmount { get; set; }
    public string AllocationMode { get; set; } = "Equal";
    public int DesiredKocCount { get; set; } = 1;
    public string PackageCode { get; set; } = string.Empty;
    public bool RequiresBooking { get; set; }
    public bool RequiresShipment { get; set; }
    public string ExecutionMode { get; set; } = "RemoteProductReview";
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ExecutionSchedule { get; set; }
    public string? BookingInstructions { get; set; }
    public string? ShippingInstructions { get; set; }
    public string? SupportDetails { get; set; }
    public string? ExecutionRequirements { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? ReviewNote { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CancellationDisputeCaseId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class CampaignStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
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
    public decimal? ProposedAmount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ApplicationStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class KocCampaignFavoriteEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public Guid CampaignId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CampaignDeliverableHistoryEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid DeliverableId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromDeliverableType { get; set; }
    public int? FromQuantity { get; set; }
    public DateTime? FromDueDateUtc { get; set; }
    public string? ToDeliverableType { get; set; }
    public int? ToQuantity { get; set; }
    public DateTime? ToDueDateUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CampaignReviewFeedbackItemEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CampaignStatusHistoryId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CampaignTargetingEntity
{
    public Guid CampaignId { get; set; }
    public string Gender { get; set; } = "Any";
    public int? MinimumFollowers { get; set; }
    public int? MaximumFollowers { get; set; }
    public decimal? MinimumEngagementRate { get; set; }
    public decimal? MaximumEngagementRate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
public sealed class CampaignTargetProvinceEntity { public Guid CampaignId { get; set; } public Guid ProvinceId { get; set; } public DateTime CreatedAtUtc { get; set; } }
public sealed class CampaignTargetCategoryEntity { public Guid CampaignId { get; set; } public Guid CategoryId { get; set; } public DateTime CreatedAtUtc { get; set; } }
public sealed class CampaignTargetingHistoryEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromSnapshotJson { get; set; }
    public string ToSnapshotJson { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ShortlistEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public DateTime? SentToBrandAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ShortlistItemEntity
{
    public Guid ShortlistId { get; set; }
    public Guid ApplicationId { get; set; }
    public string SelectionSource { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal? TargetingMatchScoreSnapshot { get; set; }
    public string? TargetingMatchDetailsJson { get; set; }
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
    public DateTime? WorkOpenedAtUtc { get; set; }
    public Guid? WorkOpenedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ParticipationStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ParticipationId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ScheduleEntity
{
    public Guid Id { get; set; }
    public Guid ParticipationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int RescheduleCount { get; set; }
    public bool IsLateCancellation { get; set; }
    public int? LateCancellationThresholdHours { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
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
    public string? SubmissionNote { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public int RevisionCount { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ScheduleReminderEventEntity
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public string ReminderCode { get; set; } = string.Empty;
    public DateTime StartsAtUtcSnapshot { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ScheduleStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ShipmentStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ShipmentEvidenceEntity
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ShipmentTrackingEventEntity
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Source { get; set; } = "ManualBrand";
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SubmissionHistoryEntity
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int RevisionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubmissionUrl { get; set; }
    public string? ReviewComment { get; set; }
    public string? SubmissionNote { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
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

public sealed class ReviewStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReviewRevisionHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public byte FromRating { get; set; }
    public byte ToRating { get; set; }
    public string? FromComment { get; set; }
    public string? ToComment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReviewReplyEntity
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReviewAppealEntity
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid AppellantUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? DecisionNote { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ReviewAppealHistoryEntity
{
    public Guid Id { get; set; }
    public Guid AppealId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReviewAppealSupplementEntity
{
    public Guid Id { get; set; }
    public Guid AppealId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
