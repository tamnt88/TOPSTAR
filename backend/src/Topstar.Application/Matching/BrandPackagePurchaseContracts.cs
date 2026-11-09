namespace Topstar.Application.Matching;

public static class BrandPackagePurchaseStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Consumed = "Consumed";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
}

public sealed record BrandPackagePurchaseItem(
    Guid Id, Guid PackagePolicyId, string OrderNumber, string PackageCode, string PackageName,
    decimal Amount, string Status, DateTime PurchasedAtUtc, DateTime? PaidAtUtc,
    DateTime? ExpiresAtUtc, Guid? CampaignId, string? CampaignTitle, string? PaymentReference,
    string PaymentMethod, string? PaymentProofUrl, string? PaymentProofNote, DateTime? PaymentProofSubmittedAtUtc)
{
    public decimal VatAmount{get;init;}public decimal VatAdded{get;init;}public decimal VatRate{get;init;}public string VatMode{get;init;}="None";
    public string? CancellationReason { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public decimal MinBudgetAmount { get; init; }
    public decimal? MaxBudgetAmount { get; init; }
    public bool BrandSelectionEnabled { get; init; }
    public int? SelectionQuota { get; init; }
    public bool CanCreateCampaign => Status == BrandPackagePurchaseStatuses.Paid
        && CampaignId is null && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > DateTime.UtcNow);
}

public sealed record BrandPackagePurchaseResult(bool Success, string Message, Guid? Id = null);

public interface IBrandPackagePurchaseService
{
    Task<IReadOnlyList<BrandPackagePurchaseItem>> GetForBrandAsync(Guid brandUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<BrandPackagePurchaseItem>> GetForAdminAsync(CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<BrandPackagePurchaseItem?> GetUsableAsync(Guid brandUserId, Guid purchaseId, CancellationToken cancellationToken = default);
    Task<BrandPackagePurchaseResult> CreateOrderAsync(Guid brandUserId, Guid packagePolicyId, string paymentMethod = "BankTransfer", CancellationToken cancellationToken = default);
    Task<BrandPackagePurchaseResult> SubmitPaymentProofAsync(Guid brandUserId, Guid purchaseId, string proofUrl, string note, CancellationToken cancellationToken = default);
    Task<BrandPackagePurchaseResult> CancelPendingAsync(Guid brandUserId, Guid purchaseId, string reason, CancellationToken cancellationToken = default);
    Task<BrandPackagePurchaseResult> ConfirmPaidAsync(Guid adminUserId, Guid purchaseId, string paymentReference, CancellationToken cancellationToken = default);
}
