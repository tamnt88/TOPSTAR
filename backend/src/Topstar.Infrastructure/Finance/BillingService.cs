using System.Data;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Campaigns;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Finance;
public sealed class BillingService(TopstarDbContext db):IBillingService
{
 public Task<IReadOnlyList<BillingInvoiceItem>> GetForAdminAsync(CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null)=>LoadAsync(null,ct,window);
 public Task<IReadOnlyList<BillingInvoiceItem>> GetForBrandAsync(Guid userId,CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null)=>LoadAsync(userId,ct,window);
 public Task<BillingInvoiceItem?> GetForBrandAsync(Guid userId,Guid invoiceId,CancellationToken ct=default)=>LoadInvoiceAsync(invoiceId,userId,ct);
 public Task<BillingInvoiceItem?> GetForAdminAsync(Guid invoiceId,CancellationToken ct=default)=>LoadInvoiceAsync(invoiceId,null,ct);
 private async Task<BillingInvoiceItem?> LoadInvoiceAsync(Guid id,Guid? brandUserId,CancellationToken ct)
 {
  var row=await(from i in db.CampaignBillingInvoices.AsNoTracking() join c in db.Campaigns.AsNoTracking() on i.CampaignId equals c.Id join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id where i.Id==id&&(!brandUserId.HasValue||b.UserId==brandUserId) select new{i,c,b}).FirstOrDefaultAsync(ct);if(row is null)return null;var history=await db.CampaignBillingInvoiceHistories.AsNoTracking().Where(x=>x.InvoiceId==id).OrderBy(x=>x.CreatedAtUtc).Select(x=>new BillingHistoryItem(x.FromStatus,x.ToStatus,x.Note,x.CreatedAtUtc)).ToListAsync(ct);return new(row.i.Id,row.c.Id,row.c.Title,row.b.CompanyName,row.i.CampaignBudgetSnapshot,row.i.FeeRate,row.i.FeeAmount,row.i.InvoiceNumber,row.i.Status,row.i.IssuedAtUtc,row.i.DueAtUtc,row.i.PaidAtUtc,row.i.PaymentReference,history);
 }
 private async Task<IReadOnlyList<BillingInvoiceItem>> LoadAsync(Guid? brandUserId,CancellationToken ct,Topstar.Application.Common.ListWindow? window=null)
 {
  var policy=await db.CampaignFeePolicies.AsNoTracking().Where(x=>x.IsActive&&x.EffectiveAtUtc<=DateTime.UtcNow).OrderByDescending(x=>x.EffectiveAtUtc).FirstOrDefaultAsync(ct)
      ?? throw new InvalidOperationException("Chưa có chính sách phí chiến dịch đang hiệu lực trong Admin.");var rate=policy.FeeRate;
  var query=from c in db.Campaigns.AsNoTracking() join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id join i0 in db.CampaignBillingInvoices.AsNoTracking() on c.Id equals i0.CampaignId into invoices from i in invoices.DefaultIfEmpty() where c.Status==CampaignStatuses.Completed&&!db.CampaignFundings.Any(f=>f.CampaignId==c.Id)&&(!brandUserId.HasValue||b.UserId==brandUserId) orderby c.UpdatedAtUtc descending select new{Campaign=c,Brand=b,Invoice=i};
  if(window is not null) window.Amounts=await query.Where(x=>x.Invoice!=null).GroupBy(x=>x.Invoice!.Status).Select(g=>new{Status=g.Key,Amount=g.Sum(x=>x.Invoice!.FeeAmount)}).ToDictionaryAsync(x=>x.Status,x=>x.Amount,ct);
  var rows=await query.ToWindowAsync(window,ct,q=>q.Where(x=>(window!.Search==null||x.Campaign.Title.Contains(window.Search)||x.Brand.CompanyName.Contains(window.Search)||(x.Invoice!=null&&x.Invoice.InvoiceNumber.Contains(window.Search)))&&(window.Status==null||(x.Invoice==null?"NotIssued":x.Invoice.Status)==window.Status)),x=>x.Invoice==null?"NotIssued":x.Invoice.Status);var ids=rows.Where(x=>x.Invoice is not null).Select(x=>x.Invoice!.Id).ToArray();var histories=await db.CampaignBillingInvoiceHistories.AsNoTracking().Where(x=>ids.Contains(x.InvoiceId)).OrderBy(x=>x.CreatedAtUtc).ToListAsync(ct);
  return rows.Select(x=>new BillingInvoiceItem(x.Invoice?.Id,x.Campaign.Id,x.Campaign.Title,x.Brand.CompanyName,x.Campaign.BudgetAmount,x.Invoice?.FeeRate??rate,x.Invoice?.FeeAmount??BillingRules.CalculateFee(x.Campaign.BudgetAmount,rate,policy.FixedFee,policy.MinimumFee,policy.MaximumFee),x.Invoice?.InvoiceNumber,x.Invoice?.Status,x.Invoice?.IssuedAtUtc,x.Invoice?.DueAtUtc,x.Invoice?.PaidAtUtc,x.Invoice?.PaymentReference,x.Invoice is null?[]:histories.Where(h=>h.InvoiceId==x.Invoice.Id).Select(h=>new BillingHistoryItem(h.FromStatus,h.ToStatus,h.Note,h.CreatedAtUtc)).ToList())).ToList();
 }
 public async Task<BillingResult> GenerateAsync(Guid actor,Guid campaignId,CancellationToken ct=default)
 {
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);var campaign=await db.Campaigns.FirstOrDefaultAsync(x=>x.Id==campaignId,ct);if(campaign is null)return new(false,"Không tìm thấy campaign.");if(campaign.Status!=CampaignStatuses.Completed)return new(false,"Chỉ phát hành phí cho campaign đã Completed.");if(await db.CampaignBillingInvoices.AnyAsync(x=>x.CampaignId==campaignId,ct))return new(false,"Campaign đã có invoice phí dịch vụ.");
  if(await db.CampaignFundings.AnyAsync(x=>x.CampaignId==campaignId,ct))return new(false,"Phí nền tảng của chiến dịch đã được thu trong yêu cầu ký quỹ.");var policy=await db.CampaignFeePolicies.AsNoTracking().Where(x=>x.IsActive).OrderByDescending(x=>x.EffectiveAtUtc).FirstAsync(ct);var now=DateTime.UtcNow;var fee=BillingRules.CalculateFee(campaign.BudgetAmount,policy.FeeRate,policy.FixedFee,policy.MinimumFee,policy.MaximumFee);var invoice=new CampaignBillingInvoiceEntity{Id=Guid.NewGuid(),CampaignId=campaignId,InvoiceNumber=$"TS-{now:yyyyMMdd}-{campaignId.ToString("N")[..8].ToUpperInvariant()}",CampaignBudgetSnapshot=campaign.BudgetAmount,FeeRate=policy.FeeRate,FeeAmount=fee,Status=BillingStatuses.Pending,IssuedAtUtc=now,DueAtUtc=now.AddDays(policy.PaymentDueDays),CreatedByUserId=actor,CreatedAtUtc=now};db.CampaignBillingInvoices.Add(invoice);db.CampaignBillingInvoiceHistories.Add(new(){Id=Guid.NewGuid(),InvoiceId=invoice.Id,ToStatus=BillingStatuses.Pending,Note=$"Phát hành phí dịch vụ theo chính sách hiệu lực {policy.EffectiveAtUtc:O}",ActorUserId=actor,CreatedAtUtc=now});try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}catch(DbUpdateException){return new(false,"Invoice vừa được phát hành bởi yêu cầu khác. Vui lòng tải lại trang.");}return new(true,"Đã phát hành invoice phí dịch vụ.",invoice.Id);
 }
 public async Task<BillingResult> ChangeStatusAsync(Guid actor,Guid id,string target,string? note,CancellationToken ct=default)
 {
  note=string.IsNullOrWhiteSpace(note)?null:note.Trim();
  var invoice=await db.CampaignBillingInvoices.FirstOrDefaultAsync(x=>x.Id==id,ct);if(invoice is null)return new(false,"Không tìm thấy invoice.");
  if(!BillingRules.CanTransition(invoice.Status,target))return new(false,$"Không thể chuyển {invoice.Status} sang {target}.");
  var now=DateTime.UtcNow;if(target==BillingStatuses.Overdue&&invoice.DueAtUtc>now)return new(false,"Invoice chưa đến hạn nên không thể đánh dấu Overdue.");
  if(target is BillingStatuses.Paid or BillingStatuses.Void&&string.IsNullOrWhiteSpace(note))return new(false,target==BillingStatuses.Paid?"Cần nhập mã tham chiếu thanh toán.":"Cần nhập lý do hủy invoice.");if(note?.Length>500)return new(false,"Nội dung không vượt quá 500 ký tự.");
  var before=invoice.Status;invoice.Status=target;invoice.UpdatedByUserId=actor;invoice.UpdatedAtUtc=now;if(target==BillingStatuses.Paid){invoice.PaidAtUtc=now;invoice.PaymentReference=note;}if(target==BillingStatuses.Void)invoice.VoidReason=note;
  db.CampaignBillingInvoiceHistories.Add(new(){Id=Guid.NewGuid(),InvoiceId=id,FromStatus=before,ToStatus=target,Note=note,ActorUserId=actor,CreatedAtUtc=now});
  try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){return new(false,"Invoice vừa được xử lý bởi Admin khác. Vui lòng tải lại trang.");}
  return new(true,"Đã cập nhật trạng thái invoice.",id);
 }
}
