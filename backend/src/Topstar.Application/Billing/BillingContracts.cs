namespace Topstar.Application.Billing;
public static class BillingStatuses{public const string Pending="Pending";public const string Paid="Paid";public const string Overdue="Overdue";public const string Void="Void";}
public static class BillingRules
{
 public const decimal PlatformFeeRate=0.15m;
 public static decimal CalculateFee(decimal campaignBudget)=>decimal.Round(campaignBudget*PlatformFeeRate,0,MidpointRounding.AwayFromZero);
 public static decimal CalculateFee(decimal budget,decimal rate,decimal fixedFee,decimal? minimum,decimal? maximum)
 {
  var fee=decimal.Round(budget*rate+fixedFee,0,MidpointRounding.AwayFromZero);
  if(minimum.HasValue&&fee<minimum.Value)fee=minimum.Value;
  if(maximum.HasValue&&fee>maximum.Value)fee=maximum.Value;
  return fee;
 }
 public static string? ValidatePolicy(decimal rate,decimal fixedFee,decimal? minimum,decimal? maximum,int holdDays,int dueDays)
  =>rate is <0 or >1?"FeeRate":fixedFee<0?"FixedFee":minimum<0?"Minimum":maximum<0?"Maximum":minimum.HasValue&&maximum.HasValue&&minimum>maximum?"Range":holdDays is <0 or >90?"HoldDays":dueDays is <1 or >90?"DueDays":null;
 public static bool CanTransition(string current,string target)=>(current,target) is (BillingStatuses.Pending,BillingStatuses.Paid) or (BillingStatuses.Pending,BillingStatuses.Void) or (BillingStatuses.Pending,BillingStatuses.Overdue) or (BillingStatuses.Overdue,BillingStatuses.Paid) or (BillingStatuses.Overdue,BillingStatuses.Void);
}
public sealed record CampaignFeePolicyItem(Guid Id,decimal FeeRate,decimal FixedFee,decimal? MinimumFee,decimal? MaximumFee,decimal GatewayFeeRate,string GatewayFeeBearer,int DisputeHoldDays,int PaymentDueDays,decimal DualApprovalThreshold,string RefundPolicy,DateTime EffectiveAtUtc,bool IsActive,IReadOnlyList<CampaignFeePolicyHistoryItem> History);
public sealed record CampaignFeePolicyHistoryItem(decimal FeeRate,decimal FixedFee,decimal? MinimumFee,decimal? MaximumFee,decimal DualApprovalThreshold,string Reason,DateTime CreatedAtUtc);
public interface IBillingService
{
 Task<IReadOnlyList<BillingInvoiceItem>> GetForAdminAsync(CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null);
 Task<IReadOnlyList<BillingInvoiceItem>> GetForBrandAsync(Guid brandUserId,CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null);
 Task<BillingInvoiceItem?> GetForBrandAsync(Guid brandUserId,Guid invoiceId,CancellationToken ct=default);
 Task<BillingInvoiceItem?> GetForAdminAsync(Guid invoiceId,CancellationToken ct=default);
 Task<BillingResult> GenerateAsync(Guid adminUserId,Guid campaignId,CancellationToken ct=default);
 Task<BillingResult> ChangeStatusAsync(Guid adminUserId,Guid invoiceId,string targetStatus,string? referenceOrReason,CancellationToken ct=default);
}
public sealed record BillingHistoryItem(string? FromStatus,string ToStatus,string? Note,DateTime CreatedAtUtc);
public sealed record BillingInvoiceItem(Guid? Id,Guid CampaignId,string CampaignTitle,string BrandName,decimal CampaignBudget,decimal FeeRate,decimal FeeAmount,string? InvoiceNumber,string? Status,DateTime? IssuedAtUtc,DateTime? DueAtUtc,DateTime? PaidAtUtc,string? PaymentReference,IReadOnlyList<BillingHistoryItem> History);
public sealed record BillingResult(bool Success,string Message,Guid? Id=null);
