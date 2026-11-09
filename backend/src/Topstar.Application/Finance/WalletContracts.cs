namespace Topstar.Application.Finance;

public static class WalletStatuses
{
    public const string Pending = "Pending";
    public const string Available = "Available";
    public const string OnHold = "OnHold";
    public const string PaidOut = "PaidOut";
    public const string Failed = "Failed";
}

public static class WalletRules
{
    public const decimal MaximumVndAmount = 9_999_999_999_999_999m;
    public static bool IsValidAmount(decimal amount)
        => amount > 0 && amount <= MaximumVndAmount && amount == decimal.Truncate(amount);
    public static bool CanTransition(string current, string target) => (current, target) switch
    {
        (WalletStatuses.Pending, WalletStatuses.Available) => true,
        (WalletStatuses.Pending, WalletStatuses.OnHold) => true,
        (WalletStatuses.OnHold, WalletStatuses.Available) => true,
        (WalletStatuses.Available, WalletStatuses.OnHold) => true,
        _ => false
    };
}

public static class PayoutStatuses
{
    public const string WithdrawRequested = "WithdrawRequested";
    public const string PaidOut = "PaidOut";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class PayoutRules
{
    public static bool CanProcess(string current, string target)
        => current == PayoutStatuses.WithdrawRequested && target is PayoutStatuses.PaidOut or PayoutStatuses.Failed;

    public static bool CanCancel(string current)
        => current == PayoutStatuses.WithdrawRequested;
}
public static class PayoutPolicyRules
{
 public const string EstimatedProcessingTime = "1–3 ngày làm việc sau khi xác nhận OTP";
 public static decimal CalculateFee(decimal amount,decimal fixedFee,decimal rate)=>decimal.Round(fixedFee+amount*rate,0,MidpointRounding.AwayFromZero);
 public static string? Validate(decimal minimum,int dailyLimit,decimal fixedFee,decimal rate)=>minimum<1?"Minimum":dailyLimit is <1 or >20?"DailyLimit":fixedFee<0?"FixedFee":rate is <0 or >0.20m?"Rate":null;
}

public static class PayoutOtpRules
{
    public const int CodeLength = 6;
    public const int MaxAttempts = 5;
    public static bool IsValidCode(string? code) => code?.Length == CodeLength && code.All(x => x is >= '0' and <= '9');
}

public interface IWalletService
{
    Task<IReadOnlyList<AdminSettlementItem>> GetAdminSettlementsAsync(CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<WalletResult> CreateEarningAsync(Guid adminUserId, Guid participationId, decimal amount, string? description, CancellationToken cancellationToken = default);
    Task<WalletResult> ChangeStatusAsync(Guid adminUserId, Guid transactionId, string targetStatus, string? reason, CancellationToken cancellationToken = default);
    Task<KocWalletOverview?> GetKocWalletAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? transactions = null, Topstar.Application.Common.ListWindow? payouts = null);
    Task<WalletResult> SavePayoutAccountAsync(Guid kocUserId, Guid? accountId, string bankCode, string accountHolderName, string accountNumber, bool makeDefault, CancellationToken cancellationToken = default);
    Task<WalletResult> SetDefaultPayoutAccountAsync(Guid kocUserId, Guid accountId, CancellationToken cancellationToken = default);
    Task<WalletResult> DeletePayoutAccountAsync(Guid kocUserId, Guid accountId, string reason, CancellationToken cancellationToken = default);
    Task<WalletResult> RequestPayoutAsync(Guid kocUserId, decimal amount, CancellationToken cancellationToken = default);
    Task<PayoutOtpChallengeResult> CreatePayoutOtpChallengeAsync(Guid kocUserId, Guid payoutAccountId, decimal amount, CancellationToken cancellationToken = default);
    Task<WalletResult> ConfirmPayoutOtpAsync(Guid kocUserId, Guid challengeId, string code, CancellationToken cancellationToken = default);
    Task<WalletResult> CancelPayoutAsync(Guid kocUserId, Guid payoutRequestId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPayoutItem>> GetAdminPayoutsAsync(CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<WalletResult> ProcessPayoutAsync(Guid adminUserId, Guid payoutRequestId, string targetStatus, string? referenceOrReason, CancellationToken cancellationToken = default);
    Task<PayoutPolicyItem> GetPayoutPolicyAsync(CancellationToken cancellationToken=default);
    Task<WalletResult> UpdatePayoutPolicyAsync(Guid adminUserId,decimal minimumAmount,int dailyRequestLimit,decimal fixedFee,decimal feeRate,string reason,CancellationToken cancellationToken=default);
}

public sealed record AdminSettlementItem(Guid ParticipationId, Guid CampaignId, string CampaignTitle, string KocName, decimal CampaignBudget, int ApprovedSubmissionCount, Guid? TransactionId, decimal? Amount, string? Status, string? HoldReason);
public sealed record StatusHistoryItem(string? FromStatus, string ToStatus, string? Note, DateTime CreatedAtUtc);
public sealed record WalletTransactionItem(Guid Id, string CampaignTitle, decimal Amount, string Status, string? Description, string? HoldReason, DateTime CreatedAtUtc, DateTime? AvailableAtUtc)
{
    public decimal? GrossAmount{get;init;}public decimal PlatformFeeAmount{get;init;}public decimal TaxAmount{get;init;}public string TaxState{get;init;}="Legacy";

    public IReadOnlyList<StatusHistoryItem> History { get; init; } = [];
}
public sealed record PayoutAccountSummary(Guid Id, string BankCode, string AccountHolderName, string MaskedAccountNumber, bool IsDefault);
public sealed record PayoutRequestItem(Guid Id, decimal Amount,decimal FeeAmount,decimal NetAmount, string Status, string BankCode, string AccountNumberLast4, string? PayoutReference, string? FailureReason, DateTime RequestedAtUtc, DateTime? ProcessedAtUtc)
{
    public IReadOnlyList<StatusHistoryItem> History { get; init; } = [];
}
public sealed record AdminPayoutItem(Guid Id, string KocName, decimal Amount,decimal FeeAmount,decimal NetAmount, string Status, string BankCode, string AccountHolderName, string AccountNumber, string? PayoutReference, string? FailureReason, DateTime RequestedAtUtc)
{
    public IReadOnlyList<StatusHistoryItem> History { get; init; } = [];
}
public sealed record PayoutPolicyItem(decimal MinimumAmount,int DailyRequestLimit,decimal FixedFee,decimal FeeRate,DateTime EffectiveAtUtc,IReadOnlyList<PayoutPolicyHistoryItem> History);
public sealed record PayoutPolicyHistoryItem(decimal MinimumAmount,int DailyRequestLimit,decimal FixedFee,decimal FeeRate,string Reason,DateTime CreatedAtUtc);
public sealed record KocWalletOverview(decimal AvailableBalance, decimal PendingBalance, decimal OnHoldBalance, decimal RequestedPayoutBalance, decimal TotalEarnings, IReadOnlyList<PayoutAccountSummary> PayoutAccounts, IReadOnlyList<WalletTransactionItem> Transactions, IReadOnlyList<PayoutRequestItem> PayoutRequests,PayoutPolicyItem Policy)
{
    public PayoutAccountSummary? PayoutAccount => PayoutAccounts.FirstOrDefault(x => x.IsDefault) ?? PayoutAccounts.FirstOrDefault();
}
public sealed record WalletResult(bool Success, string Message, Guid? Id = null);
public sealed record PayoutOtpChallengeResult(bool Success, string Message, Guid? ChallengeId = null, string? RawCode = null, string? Email = null, DateTime? ExpiresAtUtc = null);
