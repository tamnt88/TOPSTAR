using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class KocIncomeTaxModel(TopstarDbContext db):PageModel{
 [BindProperty]public bool Enabled{get;set;}
 [BindProperty]public decimal Rate{get;set;}
 [BindProperty]public decimal Threshold{get;set;}
 [BindProperty]public DateTime EffectiveAt{get;set;}=DateTime.UtcNow.AddHours(7);
 [TempData]public string? Message{get;set;}
 [TempData]public bool Success{get;set;}
 public bool Ready{get;private set;}
 public List<KocIncomeTaxPolicy> Policies{get;private set;}=[];
 public Topstar.Application.Common.ListWindow Window{get;private set;}=new();
 public List<TaxPayment> Payments{get;private set;}=[];
 public decimal Withheld{get;private set;}public decimal Remitted{get;private set;}public int Waiting{get;private set;}
 public sealed record TaxPayment(Guid Id,Guid KocId,string Name,decimal Gross,decimal Tax,decimal Fee,decimal Net,string State,DateTime CreatedAtUtc,DateTime? RemittedAt,string? Reference);
 private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task OnGetAsync(CancellationToken ct){
  Ready=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocIncomeTaxPolicies')").AnyAsync(x=>x>0,ct);if(!Ready)return;
  Policies=await db.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies").OrderByDescending(p=>p.EffectiveAtUtc).ThenByDescending(p=>p.CreatedAtUtc).Take(50).ToListAsync(ct);
  var current=Policies.FirstOrDefault(p=>p.EffectiveAtUtc<=DateTime.UtcNow);if(Request.Method=="GET"&&current is not null){Enabled=current.IsEnabled;Rate=current.Rate;Threshold=current.Threshold;}
  var all=db.WalletTransactions.AsNoTracking().Where(w=>w.TaxState=="Calculated"||w.TaxState=="Awaiting");
  Withheld=await all.SumAsync(w=>(decimal?)w.TaxAmountSnapshot,ct)??0;Remitted=await all.Where(w=>w.TaxRemittedAtUtc!=null).SumAsync(w=>(decimal?)w.TaxAmountSnapshot,ct)??0;Waiting=await all.CountAsync(w=>w.TaxState=="Awaiting",ct);
  Window=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"kocTaxPayments");Window.Total=await all.CountAsync(ct);
  var q=from w in all join k in db.KocProfiles on w.KocProfileId equals k.Id select new{w,k};
  if(Window.Search is not null)q=q.Where(x=>x.k.DisplayName.Contains(Window.Search));
  Window.Filtered=await q.CountAsync(ct);
  Payments=await q.OrderByDescending(x=>x.w.CreatedAtUtc).ThenBy(x=>x.w.Id).Skip((Window.Page-1)*Window.Size).Take(Window.Size).Select(x=>new TaxPayment(x.w.Id,x.k.Id,x.k.DisplayName,x.w.GrossAmountSnapshot??x.w.Amount,x.w.TaxAmountSnapshot,x.w.PlatformFeeAmountSnapshot,x.w.Amount,x.w.TaxState,x.w.CreatedAtUtc,x.w.TaxRemittedAtUtc,x.w.TaxRemittanceReference)).ToListAsync(ct);
 }
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){
  if(Rate<0||Rate>99)ModelState.AddModelError(nameof(Rate),"Thuế suất phải từ 0 đến 99%.");
  if(Threshold<0||Threshold!=decimal.Truncate(Threshold))ModelState.AddModelError(nameof(Threshold),"Nhập ngưỡng bằng số nguyên đồng không âm.");
  if(EffectiveAt==default)ModelState.AddModelError(nameof(EffectiveAt),"Chọn ngày hiệu lực.");
  if(!ModelState.IsValid){await OnGetAsync(ct);return Page();}
  var id=Guid.NewGuid();var now=DateTime.UtcNow;var date=DateTime.SpecifyKind(EffectiveAt,DateTimeKind.Unspecified).AddHours(-7);
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.KocIncomeTaxPolicies(Id,IsEnabled,Rate,Threshold,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({id},{Enabled},{Rate},{Threshold},{date},{now},{Actor})",ct);
  db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocIncomeTaxPolicy",EntityId=id.ToString(),ActionName="CreateVersion",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{Enabled,Rate,Threshold,date}),CreatedAtUtc=now});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu chính sách thuế KOC.";Success=true;return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Koc"});
 }
 public async Task<IActionResult> OnPostRemitAsync(Guid id,string reference,CancellationToken ct){
  reference=(reference??"").Trim();if(reference.Length==0||reference.Length>200){Message="Vui lòng nhập mã chứng từ nộp thuế.";return RedirectToPage();}
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var wallet=await db.WalletTransactions.FromSqlInterpolated($"SELECT * FROM dbo.WalletTransactions WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
  if(wallet is null||wallet.TaxState!="Calculated"||wallet.TaxAmountSnapshot<=0||wallet.TaxRemittedAtUtc.HasValue){Message="Khoản thuế không còn ở trạng thái chờ ghi nhận nộp.";return RedirectToPage();}
  wallet.TaxRemittedAtUtc=DateTime.UtcNow;wallet.TaxRemittanceReference=reference;db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocIncomeTax",EntityId=id.ToString(),ActionName="RecordTaxRemittance",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{reference,wallet.TaxAmountSnapshot}),CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã ghi nhận chứng từ nộp thuế.";Success=true;return RedirectToPage();
 }
 public async Task<IActionResult> OnPostReleaseAsync(Guid id,CancellationToken ct){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var wallet=await db.WalletTransactions.FromSqlInterpolated($"SELECT * FROM dbo.WalletTransactions WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
  if(wallet is null||wallet.TaxState!="Awaiting"||wallet.Status!="OnHold"||wallet.HoldDisputeCaseId.HasValue){Message="Khoản thu nhập không ở trạng thái chờ xác minh thuế.";return RedirectToPage();}
  wallet.Status="Pending";await db.SaveChangesAsync(ct);
  if(wallet.TaxState=="Calculated"){db.WalletTransactionStatusHistories.Add(new(){Id=Guid.NewGuid(),WalletTransactionId=id,FromStatus="OnHold",ToStatus=wallet.Status,Reason="Đã xác minh hồ sơ thuế và chốt thực nhận.",ActorUserId=Actor,CreatedAtUtc=DateTime.UtcNow});db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocIncomeTax",EntityId=id.ToString(),ActionName="ReleaseTaxHold",CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);}
  await tx.CommitAsync(ct);Success=wallet.TaxState=="Calculated";Message=Success?"Đã tính thuế và chuyển thu nhập về chờ khả dụng.":"Cần xác minh hồ sơ thuế của KOC trước.";return RedirectToPage();
 }
}
