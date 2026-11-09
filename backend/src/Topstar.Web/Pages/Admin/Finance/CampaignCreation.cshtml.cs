using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Infrastructure.Persistence;
using Topstar.Infrastructure.Finance;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class CampaignCreationModel(TopstarDbContext db):PageModel{
 [BindProperty]public CampaignCreationPolicy Input{get;set;}=new();
 public bool Ready{get;private set;}
 [TempData]public string? Message{get;set;}
 public async Task OnGetAsync(CancellationToken ct){var p=await CampaignCreationRules.ReadAsync(db,ct);Ready=p is not null;if(p is not null)Input=p;}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){var before=await CampaignCreationRules.ReadAsync(db,ct);Ready=before is not null;if(!Ready)return BadRequest();
 if(Input.MinimumBudget<=0||Input.MinimumBudget!=decimal.Truncate(Input.MinimumBudget))ModelState.AddModelError("Input.MinimumBudget","Nhập ngân sách nguyên đồng lớn hơn 0.");
 if(Input.SelfSelectionMinimumBudget<Input.MinimumBudget||Input.SelfSelectionMinimumBudget!=decimal.Truncate(Input.SelfSelectionMinimumBudget))ModelState.AddModelError("Input.SelfSelectionMinimumBudget","Ngưỡng tự chọn phải là số nguyên đồng, không thấp hơn ngân sách tối thiểu.");
 if(Input.MinimumBudgetPerKoc<=0||Input.MinimumBudgetPerKoc>Input.MinimumBudget||Input.MinimumBudgetPerKoc!=decimal.Truncate(Input.MinimumBudgetPerKoc))ModelState.AddModelError("Input.MinimumBudgetPerKoc","Nhập mức nguyên đồng từ 1 đến ngân sách tối thiểu.");
 if(Input.MaximumKocCount is <1 or >100)ModelState.AddModelError("Input.MaximumKocCount","Số KOC từ 1 đến 100.");
 Input.KocPlatformFeeRate=before!.KocPlatformFeeRate;
 if(Input.KocPlatformFeeRate<0||Input.KocPlatformFeeRate>99)ModelState.AddModelError("Input.KocPlatformFeeRate","Phí KOC từ 0 đến 99%.");
 if(!ModelState.IsValid)return Page();var actor=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var now=DateTime.UtcNow;
 await using var tx=await db.Database.BeginTransactionAsync(ct);
 await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CampaignCreationPolicies SET MinimumBudget={Input.MinimumBudget},SelfSelectionMinimumBudget={Input.SelfSelectionMinimumBudget},MinimumBudgetPerKoc={Input.MinimumBudgetPerKoc},MaximumKocCount={Input.MaximumKocCount},UpdatedAtUtc={now},ActorUserId={actor} WHERE Id=1",ct);
 db.AuditLogs.Add(new(){ActorUserId=actor,EntityName="CampaignCreationPolicy",EntityId="1",ActionName="Update",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{Before=before,After=Input}),CreatedAtUtc=now});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu cấu hình tạo chiến dịch.";return RedirectToPage();
 }
}
