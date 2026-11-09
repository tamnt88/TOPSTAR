using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Campaigns;
using Topstar.Application.Finance;
using Topstar.Application.Reporting;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Reporting;
public sealed class ReportingService(TopstarDbContext db):IReportingService
{
 public async Task<BrandReport> GetBrandReportAsync(Guid userId,CancellationToken ct=default,Guid? campaignId=null)
 {
  var brandId=await db.BrandProfiles.AsNoTracking().Where(x=>x.UserId==userId).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(ct);if(!brandId.HasValue)return new(0,0,0,0,0,0,0,0,0,null,[],[]);
  var campaigns=await db.Campaigns.AsNoTracking().Where(x=>x.BrandProfileId==brandId&&(!campaignId.HasValue||x.Id==campaignId.Value)).Select(x=>new{x.Id,x.Title,x.Status,x.BudgetAmount}).ToListAsync(ct);var ids=campaigns.Select(x=>x.Id).ToArray();
  var totals=await db.CampaignKocDailyMetrics.AsNoTracking().Where(x=>ids.Contains(x.CampaignId)).GroupBy(_=>1).Select(g=>new{Impressions=g.Sum(x=>x.Impressions),Views=g.Sum(x=>x.Views),Engagements=g.Sum(x=>x.Likes+x.Comments+x.Shares+x.Saves),Spend=g.Sum(x=>x.SpendAmount),Revenue=g.Sum(x=>x.RevenueAmount)}).FirstOrDefaultAsync(ct);
  var metricPerformance=db.CampaignKocDailyMetrics.AsNoTracking()
   .Where(x=>ids.Contains(x.CampaignId))
   .GroupBy(x=>x.KocProfileId)
   .Select(g=>new{KocId=g.Key,Impressions=g.Sum(x=>x.Impressions),Views=g.Sum(x=>x.Views),Engagements=g.Sum(x=>x.Likes+x.Comments+x.Shares+x.Saves)});
  var performance=await(from metric in metricPerformance
                        join koc in db.KocProfiles.AsNoTracking() on metric.KocId equals koc.Id
                        orderby metric.Engagements descending
                        select new{koc.Id,koc.DisplayName,metric.Impressions,metric.Views,metric.Engagements})
                       .Take(20).ToListAsync(ct);
  var kocIds=performance.Select(x=>x.Id).ToArray();var approved=await(from s in db.Submissions.AsNoTracking() join p in db.Participations.AsNoTracking() on s.ParticipationId equals p.Id where ids.Contains(p.CampaignId)&&kocIds.Contains(p.KocProfileId)&&s.Status=="Approved"&&s.IsVisible group s by p.KocProfileId into g select new{KocId=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.KocId,x=>x.Count,ct);
  var totalImpressions=totals?.Impressions??0;var totalEngagements=totals?.Engagements??0;var spend=totals?.Spend??0;var revenue=totals?.Revenue??0;return new(campaigns.Count,campaigns.Count(x=>x.Status==CampaignStatuses.Completed),campaigns.Sum(x=>x.BudgetAmount),totalImpressions,totals?.Views??0,totalEngagements,totalImpressions>0?decimal.Round(100m*totalEngagements/totalImpressions,2):0,spend,revenue,spend>0?decimal.Round(100m*(revenue-spend)/spend,2):null,campaigns.OrderByDescending(x=>x.Id).Select(x=>new ReportCampaignOption(x.Id,x.Title)).ToList(),performance.Select(x=>new BrandKocPerformance(x.DisplayName,x.Impressions,x.Views,x.Engagements,x.Impressions>0?decimal.Round(100m*x.Engagements/x.Impressions,2):0,approved.GetValueOrDefault(x.Id))).ToList());
 }
 public async Task<AdminOperationalReport> GetAdminReportAsync(CancellationToken ct=default)
 {
  var campaigns=await db.Campaigns.AsNoTracking().CountAsync(ct);var completed=await db.Campaigns.AsNoTracking().CountAsync(x=>x.Status==CampaignStatuses.Completed,ct);var pendingPayouts=await db.PayoutRequests.AsNoTracking().Where(x=>x.Status==PayoutStatuses.WithdrawRequested).GroupBy(_=>1).Select(g=>new{Count=g.Count(),Amount=g.Sum(x=>x.Amount)}).FirstOrDefaultAsync(ct);
  return new(campaigns,completed,campaigns>0?decimal.Round(100m*completed/campaigns,2):0,await db.Participations.AsNoTracking().CountAsync(x=>x.Status!="Completed",ct),await db.Shipments.AsNoTracking().CountAsync(x=>x.Status=="Issue",ct),await db.Schedules.AsNoTracking().CountAsync(x=>x.IsLateCancellation,ct),await db.DisputeCases.AsNoTracking().CountAsync(x=>x.IsActive,ct),await db.CampaignBillingInvoices.AsNoTracking().Where(x=>x.Status==BillingStatuses.Paid).SumAsync(x=>(decimal?)x.FeeAmount,ct)??0,await db.CampaignBillingInvoices.AsNoTracking().Where(x=>x.Status==BillingStatuses.Pending||x.Status==BillingStatuses.Overdue).SumAsync(x=>(decimal?)x.FeeAmount,ct)??0,pendingPayouts?.Count??0,pendingPayouts?.Amount??0);
 }
}
