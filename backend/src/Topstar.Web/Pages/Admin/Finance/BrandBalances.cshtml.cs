using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Finance;
using Topstar.Web.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class BrandBalancesModel(BrandBalanceService service,IFileStorageService storage,TopstarDbContext db):PageModel
{
 public BrandBalancePolicy Policy{get;private set;}=new();
 public bool Ready{get;private set;}=true;
 [TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public BalanceAdminSummary Summary{get;private set;}=new();
 public async Task OnGetAsync(CancellationToken ct){try{Policy=await service.PolicyAsync(ct);Summary=await SummaryAsync(ct);}catch(Microsoft.Data.SqlClient.SqlException ex)when(ex.Number is 207 or 208){Ready=false;}}
 private IQueryable<BrandWithdrawal> WithdrawalQuery()=>db.Database.SqlQuery<BrandWithdrawal>($"SELECT * FROM dbo.BrandBalanceWithdrawals");
 private async Task<BalanceAdminSummary> SummaryAsync(CancellationToken ct)
 {
  var summary=await db.Database.SqlQuery<BalanceAdminSummary>($"SELECT COALESCE(SUM(Available),0) AS Available,COALESCE(SUM(Reserved),0) AS Reserved,CAST(0 AS int) AS Pending,CAST(0 AS int) AS Approved,CAST(0 AS decimal(18,0)) AS Refundable FROM dbo.BrandBalances").SingleAsync(ct);
  summary.Pending=await WithdrawalQuery().CountAsync(x=>x.Status=="Pending",ct);summary.Approved=await WithdrawalQuery().CountAsync(x=>x.Status=="Approved",ct);
  summary.Refundable=await service.RefundSourcesQuery().SumAsync(x=>(decimal?)x.Refundable,ct)??0;
  return summary;
 }
 public async Task<IActionResult> OnGetDataAsync(string? keyword,string? status,string? scope,int draw=1,int start=0,int length=20,int orderColumn=1,string? orderDirection="desc",CancellationToken ct=default)
 {
  length=length is 10 or 20 or 50 or 100?length:20;start=Math.Clamp(start,0,10000000);keyword=(keyword??"").Trim();if(keyword.Length>200)keyword=keyword[..200];
  if(scope=="refunds")
  {
   var query=service.RefundSourcesQuery();var total=await query.CountAsync(ct);
   if(keyword.Length>0)query=query.Where(x=>x.BrandName.Contains(keyword)||x.CampaignTitle.Contains(keyword)||(x.OrderNumber!=null&&x.OrderNumber.Contains(keyword)));
   var filtered=await query.CountAsync(ct);
   var rows=await (orderColumn==3?(orderDirection=="asc"?query.OrderBy(x=>x.Refundable):query.OrderByDescending(x=>x.Refundable)):query.OrderBy(x=>x.BrandName)).ThenBy(x=>x.Id).Skip(start).Take(length).ToListAsync(ct);
   return new JsonResult(new{draw,recordsTotal=total,recordsFiltered=filtered,data=rows.Select((x,i)=>new{rowNumber=start+i+1,x.Id,x.BrandName,x.CampaignTitle,x.OrderNumber,x.Refundable}),summary=await SummaryAsync(ct)});
  }
  var withdrawals=WithdrawalQuery();var all=await withdrawals.CountAsync(ct);
  if(status is "Pending" or "Approved" or "Paid" or "Rejected" or "Cancelled")withdrawals=withdrawals.Where(x=>x.Status==status);
  if(keyword.Length>0){var brands=db.BrandProfiles.Where(b=>b.CompanyName.Contains(keyword)).Select(b=>b.Id);withdrawals=withdrawals.Where(x=>brands.Contains(x.BrandProfileId)||x.AccountHolder.Contains(keyword)||x.BankName.Contains(keyword)||(x.PaymentReference!=null&&x.PaymentReference.Contains(keyword)));}
  var matches=await withdrawals.CountAsync(ct);
  var sorted=orderColumn==3?(orderDirection=="asc"?withdrawals.OrderBy(x=>x.Amount):withdrawals.OrderByDescending(x=>x.Amount)):(orderDirection=="asc"?withdrawals.OrderBy(x=>x.CreatedAtUtc):withdrawals.OrderByDescending(x=>x.CreatedAtUtc));
  var items=await sorted.ThenBy(x=>x.Id).Skip(start).Take(length).ToListAsync(ct);var ids=items.Select(x=>x.BrandProfileId).Distinct().ToArray();var names=await db.BrandProfiles.Where(b=>ids.Contains(b.Id)).ToDictionaryAsync(b=>b.Id,b=>b.CompanyName,ct);
  return new JsonResult(new{draw,recordsTotal=all,recordsFiltered=matches,data=items.Select((x,i)=>new{rowNumber=start+i+1,x.Id,brandName=names.GetValueOrDefault(x.BrandProfileId),createdAtUtc=DateTime.SpecifyKind(x.CreatedAtUtc,DateTimeKind.Utc),x.Amount,x.Fee,x.NetAmount,x.BankName,accountTail=x.AccountNumber[^Math.Min(4,x.AccountNumber.Length)..],x.Status,x.FirstApprovedBy,x.DualApprovalRequired}),summary=await SummaryAsync(ct)});
 }
 public async Task<IActionResult> OnGetDetailAsync(Guid id,CancellationToken ct)
 {
  var request=await WithdrawalQuery().SingleOrDefaultAsync(x=>x.Id==id,ct);if(request is null)return NotFound();
  var name=await db.BrandProfiles.Where(b=>b.Id==request.BrandProfileId).Select(b=>b.CompanyName).SingleAsync(ct);
  return Partial("_BrandWithdrawalReview",new BrandWithdrawalReview(request,name,Actor()));
 }
 public async Task<IActionResult> OnGetApprovalsAsync(CancellationToken ct)
 {
  var approvals=await service.CreditApprovalsAsync(ct);var fundingIds=approvals.Select(x=>x.FundingId).ToArray();
  var sources=await service.RefundSourcesQuery().Where(x=>fundingIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
  return new JsonResult(approvals.Where(x=>sources.ContainsKey(x.FundingId)&&x.Amount<=sources[x.FundingId].Refundable).Select(x=>new{x.Operation,x.FundingId,x.Amount,brandName=sources[x.FundingId].BrandName,campaignTitle=sources[x.FundingId].CampaignTitle,canApprove=x.FirstApprover!=Actor()}));
 }
 public async Task<IActionResult> OnPostPolicyAsync(bool enabled,decimal minimum,decimal fee,decimal dual,int days,CancellationToken ct)
 {
  if(!ModelState.IsValid)return Done(new(false,"Thông số chính sách không hợp lệ."));
  return Done(await service.SavePolicyAsync(Actor(),enabled,minimum,fee,dual,days,ct));
 }
 public async Task<IActionResult> OnPostCreditAsync(Guid fundingId,string amount,Guid operation,CancellationToken ct)
 {
  if(!CampaignFundingRules.TryParseVnd(amount,out var value))return Done(new(false,"Số tiền hoàn không hợp lệ."));
  return Done(await service.CreditRefundAsync(Actor(),fundingId,value,operation,ct));
 }
 public async Task<IActionResult> OnPostProcessAsync(Guid id,string action,string? reference,string? note,IFormFile? evidenceFile,CancellationToken ct)
 {
  string? url=null;var committed=false;
  try
  {
   if(note?.Length>500)return Done(new(false,"Ghi chú tối đa 500 ký tự."));
   if(action=="Paid"){url=await storage.SaveCampaignFundingEvidenceAsync(evidenceFile,ct);if(url is null)return Done(new(false,"Chọn ảnh chứng từ JPG, PNG hoặc WEBP tối đa 5 MB."));}
   var result=await service.ProcessWithdrawalAsync(Actor(),id,action,reference,url,note,true,ct);committed=result.Success;return Done(result);
  }
  finally{if(url is not null&&!committed)await storage.DeleteCampaignFundingEvidenceAsync(url,CancellationToken.None);}
 }
 private IActionResult Done(BrandBalanceResult result){if(Request.Headers.XRequestedWith=="XMLHttpRequest")return new JsonResult(new{success=result.Success,message=result.Message});Message=result.Message;Success=result.Success;return RedirectToPage();}
}
public sealed class BalanceAdminSummary
{
 public decimal Available{get;set;}public decimal Reserved{get;set;}public int Pending{get;set;}public int Approved{get;set;}public decimal Refundable{get;set;}
}
public sealed record BrandWithdrawalReview(BrandWithdrawal Request,string BrandName,Guid Actor);
