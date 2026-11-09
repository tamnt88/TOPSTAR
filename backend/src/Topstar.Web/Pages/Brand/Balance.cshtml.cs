using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Finance;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles="Brand")]
public sealed class BalanceModel(BrandBalanceService service,TopstarDbContext db):PageModel
{
 public BrandBalanceRow Balance{get;private set;}=new();
 public BrandBalancePolicy Policy{get;private set;}=new();
 public IReadOnlyList<BrandWithdrawal> Withdrawals{get;private set;}=[];
 public IReadOnlyList<BrandBalanceEntry> Entries{get;private set;}=[];
 public bool Ready{get;private set;}=true;
 [TempData]public string? Message{get;set;} [TempData]public bool Success{get;set;}
 private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task OnGetAsync(CancellationToken ct)
 {
  try{Balance=await service.GetAsync(UserId(),ct);Policy=await service.PolicyAsync(ct);}
  catch(Microsoft.Data.SqlClient.SqlException ex)when(ex.Number is 208 or 207){Ready=false;}
 }
 public async Task<IActionResult> OnGetDataAsync(string? scope,string? keyword,string? status,int draw=1,int start=0,int length=20,CancellationToken ct=default)
 {
  var brand=await service.BrandIdAsync(UserId(),ct);if(!brand.HasValue)return Forbid();
  start=Math.Clamp(start,0,10000000);length=length is 10 or 20 or 50 or 100?length:20;keyword=(keyword??"").Trim();if(keyword.Length>200)keyword=keyword[..200];
  if(scope=="history"){
   var query=db.Database.SqlQuery<BrandBalanceEntry>($"SELECT * FROM dbo.BrandBalanceLedgers WHERE BrandProfileId={brand.Value}");var total=await query.CountAsync(ct);if(keyword.Length>0)query=query.Where(x=>x.Note.Contains(keyword));var filtered=await query.CountAsync(ct);
   var rows=await query.OrderByDescending(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).Skip(start).Take(length).ToListAsync(ct);
   return new JsonResult(new{draw,recordsTotal=total,recordsFiltered=filtered,data=rows.Select(x=>new{x.Id,x.Note,x.Amount,x.AvailableAfter,x.ReservedAfter,createdAtUtc=DateTime.SpecifyKind(x.CreatedAtUtc,DateTimeKind.Utc)})});
  }
  var requests=db.Database.SqlQuery<BrandWithdrawal>($"SELECT * FROM dbo.BrandBalanceWithdrawals WHERE BrandProfileId={brand.Value}");var all=await requests.CountAsync(ct);if(status is "Pending" or "Approved" or "Paid" or "Cancelled" or "Rejected")requests=requests.Where(x=>x.Status==status);if(keyword.Length>0)requests=requests.Where(x=>x.BankName.Contains(keyword)||(x.PaymentReference!=null&&x.PaymentReference.Contains(keyword)));var count=await requests.CountAsync(ct);
  var items=await requests.OrderByDescending(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).Skip(start).Take(length).ToListAsync(ct);
  return new JsonResult(new{draw,recordsTotal=all,recordsFiltered=count,data=items.Select(x=>new{x.Id,x.Amount,x.Fee,x.NetAmount,x.BankName,accountTail=x.AccountNumber[^Math.Min(4,x.AccountNumber.Length)..],x.Status,createdAtUtc=DateTime.SpecifyKind(x.CreatedAtUtc,DateTimeKind.Utc)})});
 }
 public async Task<IActionResult> OnPostWithdrawAsync(string amount,Guid operation,CancellationToken ct)
 {
  if(!CampaignFundingRules.TryParseVnd(amount,out var value)){Message="Số tiền phải là số nguyên đồng hợp lệ.";Success=false;}
  else{var result=await service.RequestWithdrawalAsync(UserId(),value,operation,ct);Message=result.Message;Success=result.Success;}
  return RedirectToPage();
 }
 public async Task<IActionResult> OnPostCancelAsync(Guid id,CancellationToken ct)
 {
  var result=await service.ProcessWithdrawalAsync(UserId(),id,"Cancel",null,null,null,false,ct);Message=result.Message;Success=result.Success;return RedirectToPage();
 }
 public static string Label(string status)=>status switch{"Pending"=>"Chờ duyệt","Approved"=>"Đã duyệt, chờ chuyển tiền","Paid"=>"Đã chuyển tiền","Rejected"=>"Đã từ chối","Cancelled"=>"Đã hủy",_=>status};
}
