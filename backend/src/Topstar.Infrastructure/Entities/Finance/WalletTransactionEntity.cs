namespace Topstar.Infrastructure.Entities;

public sealed class WalletTransactionEntity
{
 public decimal PlatformFeeRateSnapshot {get;set;}
 public decimal PlatformFeeAmountSnapshot {get;set;}

 public decimal? GrossAmountSnapshot{get;set;}public decimal TaxAmountSnapshot{get;set;}public decimal TaxRateSnapshot{get;set;}public Guid? TaxPolicyIdSnapshot{get;set;}public string TaxState{get;set;}="Legacy";public string? TaxTreatmentSnapshot{get;set;}public string? TaxRemittanceReference{get;set;}public DateTime? TaxRemittedAtUtc{get;set;}

    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ParticipationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HoldReason { get; set; }
    public Guid? HoldDisputeCaseId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? AvailableAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocPayoutAccountEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumberCiphertext { get; set; } = string.Empty;
    public string AccountNumberLast4 { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PayoutRequestEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmountSnapshot { get; set; }
    public decimal NetAmountSnapshot { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumberCiphertext { get; set; } = string.Empty;
    public string AccountNumberLast4 { get; set; } = string.Empty;
    public string? PayoutReference { get; set; }
    public string? FailureReason { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PayoutPolicyEntity{public Guid Id{get;set;}public string Code{get;set;}="DEFAULT";public decimal MinimumAmount{get;set;}public int DailyRequestLimit{get;set;}public decimal FixedFee{get;set;}public decimal FeeRate{get;set;}public DateTime EffectiveAtUtc{get;set;}public Guid? UpdatedByUserId{get;set;}public DateTime CreatedAtUtc{get;set;}public DateTime? UpdatedAtUtc{get;set;}}
public sealed class PayoutPolicyHistoryEntity{public Guid Id{get;set;}public Guid PolicyId{get;set;}public decimal MinimumAmount{get;set;}public int DailyRequestLimit{get;set;}public decimal FixedFee{get;set;}public decimal FeeRate{get;set;}public string Reason{get;set;}=string.Empty;public Guid? ActorUserId{get;set;}public DateTime CreatedAtUtc{get;set;}}
public sealed class PayoutOtpChallengeEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public decimal Amount { get; set; }
    public Guid? PayoutAccountId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WalletTransactionStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid WalletTransactionId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? DisputeCaseId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PayoutRequestStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PayoutRequestId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
