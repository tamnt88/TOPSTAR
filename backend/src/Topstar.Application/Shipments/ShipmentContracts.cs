namespace Topstar.Application.Shipments;

public static class ShipmentStatuses
{
    public const string NotCreated = "NotCreated";
    public const string Created = "Created";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Issue = "Issue";
    public const string Returned = "Returned";
}

public static class ShipmentTransitions
{
    public static bool CanDispatch(string status) => status is ShipmentStatuses.Created or ShipmentStatuses.Returned;
    public static bool CanConfirmDelivered(string status) => status == ShipmentStatuses.InTransit;
    public static bool CanReportIssue(string status) => status is ShipmentStatuses.Created or ShipmentStatuses.InTransit;
    public static bool CanResolveIssue(string status, string decision) => status == ShipmentStatuses.Issue && decision is ShipmentStatuses.InTransit or ShipmentStatuses.Returned;
}

public static class ShipmentEvidenceRules
{
    public const int MaxPerRole = 10;
    public static bool IsAllowedRole(string? role) => role is "BRAND" or "KOC";
    public static bool IsValidReceiptNote(string? note)
        => !string.IsNullOrWhiteSpace(note) && note.Trim().Length is >= 10 and <= 500;
    public static bool IsValidIssueNote(string? note)
        => !string.IsNullOrWhiteSpace(note) && note.Trim().Length is >= 10 and <= 1000;
    public static bool IsValidEvidenceCaption(string? caption)
        => !string.IsNullOrWhiteSpace(caption) && caption.Trim().Length is >= 10 and <= 500;
    public static bool IsValidStoredUrl(string? url)
        => url?.StartsWith("/uploads/shipment-evidence/", StringComparison.OrdinalIgnoreCase) == true && url.Length <= 1000;
    public static bool CanAdd(int existingForRole) => existingForRole >= 0 && existingForRole < MaxPerRole;
}

public static class ShipmentTrackingRules
{
    public static readonly string[] EventCodes = ["PickedUp", "AtHub", "OutForDelivery", "DeliveryAttempt", "Exception", "Other"];
    public static bool IsValidEventCode(string? code) => code is not null && EventCodes.Contains(code, StringComparer.Ordinal);
    public static bool CanAdd(string shipmentStatus) => shipmentStatus is ShipmentStatuses.InTransit or ShipmentStatuses.Issue;
}

public sealed record ShipmentHistoryItem(string? FromStatus, string ToStatus, string? Note, DateTime CreatedAtUtc);
public sealed record ShipmentEvidenceItem(Guid Id, string EvidenceType, string FileUrl, string? Caption, string ActorRole, DateTime CreatedAtUtc);
public sealed record ShipmentTrackingItem(Guid Id, string EventCode, string Description, string? Location, DateTime OccurredAtUtc, string Source, DateTime CreatedAtUtc);

public sealed record ShipmentWorkItem(
    Guid? ShipmentId, Guid ParticipationId, string CampaignTitle, string KocName, string ReceiverName,
    string ReceiverPhone, string Address, string? Carrier, string? TrackingCode, string Status,
    string? IssueNote, bool RequiresShipment, IReadOnlyList<ShipmentHistoryItem> History, IReadOnlyList<ShipmentEvidenceItem> Evidence, IReadOnlyList<ShipmentTrackingItem> Tracking);

public sealed record ShipmentResult(bool Success, string Message);

public sealed class ShipmentConcurrencyException : Exception
{
    public ShipmentConcurrencyException() : base("Trạng thái vận đơn vừa được cập nhật ở phiên khác. Vui lòng tải lại trang.") { }
}

public interface IShipmentWorkflowService
{
    Task<IReadOnlyList<ShipmentWorkItem>> GetBrandItemsAsync(Guid brandUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<ShipmentWorkItem>> GetBrandItemsForCampaignAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<ShipmentWorkItem>> GetKocItemsAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<ShipmentResult> CreateAsync(Guid brandUserId, Guid participationId, CancellationToken cancellationToken = default);
    Task<ShipmentResult> DispatchAsync(Guid brandUserId, Guid shipmentId, string carrier, string trackingCode, CancellationToken cancellationToken = default);
    Task<ShipmentResult> ReportIssueAsync(Guid kocUserId, Guid shipmentId, string issueNote, CancellationToken cancellationToken = default);
    Task<ShipmentResult> ConfirmDeliveredAsync(Guid kocUserId, Guid shipmentId, string evidenceUrl, string receiptNote, CancellationToken cancellationToken = default);
    Task<ShipmentResult> ResolveIssueAsync(Guid brandUserId, Guid shipmentId, string decision, string note, CancellationToken cancellationToken = default);
    Task<ShipmentResult> AddEvidenceAsync(Guid userId, Guid shipmentId, string actorRole, string fileUrl, string? caption, CancellationToken cancellationToken = default);
    Task<ShipmentResult> AddTrackingEventAsync(Guid brandUserId, Guid shipmentId, string eventCode, string description, string? location, DateTime occurredAtLocal, CancellationToken cancellationToken = default);
}
