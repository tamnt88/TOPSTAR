namespace Topstar.Application.Billing;

public static class CampaignFundingStatuses{public const string Draft="Draft";public const string Pending="Pending";public const string Paid="Paid";public const string PartiallyReleased="PartiallyReleased";public const string Released="Released";public const string Cancelled="Cancelled";public const string Refunded="Refunded";}
public static class CampaignAllocationModes{public const string Equal="Equal";public const string Custom="Custom";public const string Prepaid="Prepaid";}
public static class CampaignPaymentMethods{public const string BankTransfer="BankTransfer";public const string OnePay="OnePay";public const string VnPay="VnPay";}
public static class CampaignFundingEvidenceTypes{public const string BrandPayment="BrandPayment";public const string Reconciliation="Reconciliation";public const string Refund="Refund";}
public static class CampaignFundingEvidenceReviewStatuses{public const string Accepted="Accepted";public const string SupplementRequired="SupplementRequired";public const string Rejected="Rejected";}
public static class CampaignFundingRules
{
 public static bool IsValidPrepayment(decimal amount, decimal remainingBudget)
     => amount > 0 && decimal.Truncate(amount) == amount && amount <= remainingBudget;
 public static bool TryParseVnd(string? raw,out decimal amount)
 {
  amount=0;raw=raw?.Trim();
  return raw is not null && raw.Length<=32 && System.Text.RegularExpressions.Regex.IsMatch(raw,@"^(?:[0-9]+|[1-9][0-9]{0,2}(?:\.[0-9]{3})+)$")
   && decimal.TryParse(raw.Replace(".",""),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out amount);
 }
 public static string? ValidateEligibility(string status,decimal budget,bool finalized,int desired,int confirmed)
 {
  if(status is not (Campaigns.CampaignStatuses.Published or Campaigns.CampaignStatuses.InProgress))return "Chiến dịch chưa được phê duyệt hoặc đã kết thúc, không thể phân bổ thù lao mới.";
  if(confirmed<=0)return "Chưa có KOC được xác nhận. Vui lòng xác nhận danh sách KOC trước khi phân bổ thù lao.";
  if(desired<=0)return "Số lượng KOC của chiến dịch không hợp lệ.";
  if(budget<=0||budget!=decimal.Truncate(budget)||budget<confirmed)return "Ngân sách phải là số tiền nguyên đồng và đủ để phân bổ thù lao lớn hơn 0 cho mỗi KOC.";
  return null;
 }
 public static IReadOnlyDictionary<Guid,decimal> SplitEqual(decimal budget,IReadOnlyList<Guid> ids){if(budget<=0||ids.Count==0)return new Dictionary<Guid,decimal>();var normalized=decimal.Round(budget,0,MidpointRounding.AwayFromZero);var baseAmount=decimal.Floor(normalized/ids.Count);var remainder=(int)(normalized-baseAmount*ids.Count);var result=new Dictionary<Guid,decimal>();for(var i=0;i<ids.Count;i++)result[ids[i]]=baseAmount+(i<remainder?1m:0m);return result;}
 public static string? ValidateAllocations(decimal budget,IReadOnlyDictionary<Guid,decimal> values,IReadOnlyCollection<Guid> expected)=>budget<=0||budget!=decimal.Truncate(budget)?"Ngân sách phải lớn hơn 0 và là số tiền nguyên đồng.":expected.Count==0?"Chưa có KOC được xác nhận để phân bổ thù lao.":values.Count!=expected.Count||values.Keys.Any(x=>!expected.Contains(x))?"Danh sách phân bổ không khớp danh sách KOC đã xác nhận.":values.Values.Any(x=>x<=0||x!=decimal.Truncate(x))?"Thù lao của mỗi KOC phải lớn hơn 0 và là số tiền nguyên đồng.":values.Values.Sum()!=budget?"Tổng thù lao phải bằng ngân sách chiến dịch.":null;
}
public sealed record CampaignFundingAllocationItem(Guid Id,Guid ParticipationId,string KocName,decimal Amount,decimal AcceptedAmount,decimal RefundedAmount,string Status,DateTime? ReleasedAtUtc);
public sealed record CampaignFundingItem(Guid? Id,Guid CampaignId,int VersionNo,string CampaignTitle,string BrandName,decimal CampaignBudget,string AllocationMode,string Status,string? OrderNumber,string PaymentMethod,decimal FeeRateSnapshot,decimal FixedFeeSnapshot,decimal PlatformFeeSnapshot,decimal GatewayFeeRateSnapshot,decimal GatewayFeeSnapshot,string GatewayFeeBearerSnapshot,decimal TotalPayableSnapshot,int DisputeHoldDaysSnapshot,int PaymentDueDaysSnapshot,string RefundPolicySnapshot,DateTime? SubmittedAtUtc,DateTime? DueAtUtc,DateTime? PaidAtUtc,string? PaymentReference,string? PaymentProofUrl,string? PaymentProofNote,DateTime? PaymentProofSubmittedAtUtc,DateTime? CancelledAtUtc,string? CancellationReason,decimal RefundedAmount,string? RefundReference,string? RefundBankName,string? RefundAccountNumber,string? RefundAccountHolder,IReadOnlyList<CampaignFundingAllocationItem> Allocations)
{
 public int ConfirmedKocCount {get;init;}
 public decimal VatAmount{get;init;}public decimal VatAdded{get;init;}public decimal VatRate{get;init;}public string VatMode{get;init;}="None";
    public decimal BalanceAppliedAmount {get;init;}
 public DateTime? OnlinePaymentStartedAtUtc {get;init;}
 public decimal RemainingPayment => Math.Max(0,TotalPayableSnapshot-BalanceAppliedAmount);
 public int DesiredKocCount {get;init;}
 public bool IsShortlistFinalized {get;init;}
 public string CampaignStatus {get;init;} = "";
 public bool SavedAllocationsMatch {get;init;}
 public decimal TotalCampaignBudget {get;init;}
 public decimal PreviouslyAllocatedBudget {get;init;}
 public bool IsAllocatingPrepaidReserve {get;init;}
 public decimal RemainingCampaignBudget => Math.Max(0,TotalCampaignBudget-PreviouslyAllocatedBudget-(Status==CampaignFundingStatuses.Draft&&Id.HasValue?CampaignBudget:0));
 public decimal AvailableToPrepay => RemainingCampaignBudget;
 public decimal EligibilityBudget => TotalCampaignBudget > 0 ? Math.Max(0,TotalCampaignBudget-PreviouslyAllocatedBudget) : CampaignBudget;
 public string? EligibilityError => CampaignFundingRules.ValidateEligibility(CampaignStatus,EligibilityBudget,IsShortlistFinalized,DesiredKocCount,ConfirmedKocCount);
 public bool CanEditAllocations => IsAllocatingPrepaidReserve || Status is (CampaignFundingStatuses.Draft or CampaignFundingStatuses.Cancelled) && AllocationMode!=CampaignAllocationModes.Prepaid && EligibilityError is null;
 public bool CanSubmit => Status==CampaignFundingStatuses.Draft && Id.HasValue && (AllocationMode==CampaignAllocationModes.Prepaid
  ? CampaignBudget>0 && CampaignBudget==decimal.Truncate(CampaignBudget)
  : CanEditAllocations && SavedAllocationsMatch && CampaignFundingRules.ValidateAllocations(CampaignBudget,Allocations.ToDictionary(x=>x.ParticipationId,x=>x.Amount),Allocations.Select(x=>x.ParticipationId).ToArray()) is null);
}
public sealed record CampaignFundingResult(bool Success,string Message,Guid? Id=null);
public interface ICampaignFundingService
{
 Task<CampaignFeePolicyItem> GetPolicyAsync(CancellationToken ct=default);
 Task<CampaignFundingResult> UpdatePolicyAsync(Guid adminUserId,decimal rate,decimal fixedFee,decimal? minimum,decimal? maximum,decimal gatewayRate,string gatewayBearer,int holdDays,int dueDays,decimal dualApprovalThreshold,string refundPolicy,DateTime effectiveAtUtc,bool isActive,string reason,CancellationToken ct=default);
 Task<IReadOnlyList<CampaignFundingItem>> GetForBrandAsync(Guid brandUserId,CancellationToken ct=default,Topstar.Application.Common.ListWindow? window=null);
 Task<IReadOnlyList<CampaignFundingItem>> GetForAdminAsync(CancellationToken ct=default,Topstar.Application.Common.ListWindow? window=null);
 Task<CampaignFundingItem?> GetForBrandAsync(Guid brandUserId,Guid campaignId,CancellationToken ct=default);
 Task<CampaignFundingResult> SaveAllocationsAsync(Guid brandUserId,Guid campaignId,string mode,IReadOnlyDictionary<Guid,decimal> custom,CancellationToken ct=default);
 Task<CampaignFundingResult> PrepareFullFundingAsync(Guid brandUserId,Guid campaignId,CancellationToken ct=default,decimal? amount=null);
 Task<CampaignFundingResult> SubmitAsync(Guid brandUserId,Guid campaignId,string paymentMethod,CancellationToken ct=default,decimal? expectedTotal=null);
 Task<CampaignFundingResult> SubmitPaymentProofAsync(Guid brandUserId,Guid fundingId,string proofUrl,string note,CancellationToken ct=default);
 Task<CampaignFundingResult> ConfirmPaidAsync(Guid adminUserId,Guid fundingId,string reference,string? manualReason=null,CancellationToken ct=default);
 Task<CampaignFundingResult> CancelPendingAsync(Guid brandUserId,Guid fundingId,string reason,CancellationToken ct=default);
 Task<CampaignFundingResult> SettleAllocationAsync(Guid adminUserId,Guid allocationId,decimal acceptedAmount,string reason,CancellationToken ct=default);
 Task<CampaignFundingResult> ConfirmRefundAsync(Guid adminUserId,Guid fundingId,decimal amount,string reference,string reason,CancellationToken ct=default);
 Task<CampaignFundingResult> AddAdminEvidenceAsync(Guid adminUserId,Guid fundingId,string evidenceType,string fileUrl,string reference,decimal? amount,string note,CancellationToken ct=default);
 Task<CampaignFundingResult> ReviewEvidenceAsync(Guid adminUserId,Guid evidenceId,string status,string reason,CancellationToken ct=default);
 Task<bool> IsFundedAsync(Guid campaignId,CancellationToken ct=default);
 Task<CampaignFundingResult> CreateEarningsAsync(Guid actorUserId,Guid campaignId,CancellationToken ct=default);
}
