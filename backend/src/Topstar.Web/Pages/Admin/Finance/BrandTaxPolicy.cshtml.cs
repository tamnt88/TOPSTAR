using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class BrandTaxPolicyModel(TopstarDbContext db):PageModel{
 [BindProperty(SupportsGet=true)]public string Scope{get;set;}="Package";
 [BindProperty]public string VatMode{get;set;}="None";
 [BindProperty]public decimal VatRate{get;set;}
 [BindProperty]public string TaxBase{get;set;}="ServiceFee";
 [BindProperty]public DateTime EffectiveAt{get;set;}=DateTime.UtcNow.AddHours(7);
 public bool Ready{get;private set;}
 public List<BrandTaxPolicy> History{get;private set;}=[];
 public BrandTaxPolicy? Current{get;private set;}
 public Topstar.Application.Common.ListWindow Window{get;private set;}=new();
 [TempData]public string? Message{get;set;}
 public async Task OnGetAsync(CancellationToken ct){
  if(Request.Method=="GET" && Request.Path.Value?.Equals("/Admin/Finance/BrandTaxPolicy",StringComparison.OrdinalIgnoreCase)==true){Response.Redirect("/Admin/Finance/TaxFees?tab=Brand");return;}
  if(Request.Method=="GET"&&Scope is not ("Package" or "Funding"))Scope="Package";
  Ready=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.BrandTaxPolicies')").AnyAsync(x=>x>0,ct);if(!Ready)return;
  var query=db.Database.SqlQuery<BrandTaxPolicy>($"SELECT * FROM dbo.BrandTaxPolicies WHERE Scope={Scope} OR ({Scope}=N'Funding' AND Scope=N'Package')");var now=DateTime.UtcNow;
  Current=await BrandTaxPricing.ReadEffectivePolicyAsync(db,Scope,ct);
  Window=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandTaxHistory");Window.Total=await query.CountAsync(ct);Window.Filtered=Window.Total;Window.Page=Math.Min(Window.Page,Window.Pages);
  History=await query.OrderByDescending(p=>p.CreatedAtUtc).Skip((Window.Page-1)*Window.Size).Take(Window.Size).ToListAsync(ct);
  if(Request.Method=="GET"&&Current is not null){VatMode=Current.VatMode;VatRate=Current.VatRate;TaxBase=Current.TaxBase;}
 }
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){
  if(Scope is not ("Package" or "Funding"))ModelState.AddModelError(nameof(Scope),"Chọn nghiệp vụ.");
  if(VatMode is not ("None" or "Included" or "Excluded"))ModelState.AddModelError(nameof(VatMode),"Chọn cách tính thuế.");
  if(VatRate<0||VatRate>100)ModelState.AddModelError(nameof(VatRate),"Thuế suất phải từ 0 đến 100%.");
  if(TaxBase is not ("ServiceFee" or "Total"))ModelState.AddModelError(nameof(TaxBase),"Chọn cơ sở tính thuế.");
  if(EffectiveAt==default)ModelState.AddModelError(nameof(EffectiveAt),"Chọn thời điểm áp dụng.");
  if(!ModelState.IsValid){await OnGetAsync(ct);return Page();}
  await OnGetAsync(ct);if(!Ready)return Page();
  var id=Guid.NewGuid();var now=DateTime.UtcNow;var effective=DateTime.SpecifyKind(EffectiveAt,DateTimeKind.Unspecified).AddHours(-7);var actor=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);if(Scope=="Package")TaxBase="Total";if(VatMode=="None")VatRate=0;
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandTaxPolicies(Id,Scope,VatMode,VatRate,TaxBase,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({id},{Scope},{VatMode},{VatRate},{TaxBase},{effective},{now},{actor})",ct);
  db.AuditLogs.Add(new(){ActorUserId=actor,EntityName="BrandTaxPolicy",EntityId=id.ToString(),ActionName="CreateVersion",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{Scope,VatMode,VatRate,TaxBase,effective}),CreatedAtUtc=now});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu cấu hình thuế Brand.";return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Brand"});
 }
}
