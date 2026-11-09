using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Finance;
public sealed class CampaignCreationPolicy {
 public int Id {get;set;}
 public decimal MinimumBudget {get;set;}
 public decimal SelfSelectionMinimumBudget {get;set;}
 public decimal MinimumBudgetPerKoc {get;set;}
 public int MaximumKocCount {get;set;}
 public decimal KocPlatformFeeRate {get;set;}
 public DateTime UpdatedAtUtc {get;set;}
 public Guid? ActorUserId {get;set;}
}
public static class CampaignCreationRules {
 public static async Task<CampaignCreationPolicy?> ReadAsync(TopstarDbContext db,CancellationToken ct=default) {
  if(!await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.CampaignCreationPolicies')").AnyAsync(x=>x>0,ct))return null;
  return await db.Database.SqlQuery<CampaignCreationPolicy>($"SELECT * FROM dbo.CampaignCreationPolicies WHERE Id=1").SingleOrDefaultAsync(ct);
 }
 public static string? Validate(CampaignCreationPolicy p,decimal budget,int count,bool selfSelect) {
  string Money(decimal amount)=>amount.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" đ";
  if(budget>9_999_999_999_999_999m)return "Ngân sách vượt giới hạn cho phép. Vui lòng nhập số tiền nhỏ hơn.";
  if(budget!=decimal.Truncate(budget))return "Vui lòng nhập số tiền tròn đồng, không có phần thập phân.";
  if(budget<p.MinimumBudget)return $"Ngân sách tối thiểu đang được cấu hình là {Money(p.MinimumBudget)}. Vui lòng tăng ngân sách.";
  if(count<1||count>p.MaximumKocCount)return $"Vui lòng chọn từ 1 đến {p.MaximumKocCount} KOC.";
  if(budget/count<p.MinimumBudgetPerKoc)return $"Với {count} KOC, ngân sách cần ít nhất {Money(count*p.MinimumBudgetPerKoc)}. Bạn có thể tăng ngân sách hoặc giảm số KOC.";
  if(selfSelect&&budget<p.SelfSelectionMinimumBudget)return $"Brand tự chọn KOC áp dụng cho ngân sách từ {Money(p.SelfSelectionMinimumBudget)}. Vui lòng tăng ngân sách hoặc chọn TopStar phân bổ KOC.";
  return null;
 }
 public static Topstar.Application.Matching.PackagePolicyItem? ReferenceTemplate(CampaignCreationPolicy policy,Topstar.Application.Matching.PackagePolicyItem template){
  if(template.ReferenceBudget is not >0)return null;
  var count=template.SelectionQuota??1;
  if(Validate(policy,template.ReferenceBudget.Value,count,template.BrandSelectionEnabled)is not null)return null;
  return template with{ReferenceBudgetIsSuggested=false};
 }
 public static int MaximumKoc(CampaignCreationPolicy p,decimal budget)=>(int)Math.Min(p.MaximumKocCount,Math.Max(0,decimal.Floor(budget/p.MinimumBudgetPerKoc)));
}
