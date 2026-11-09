using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class CampaignPreparationModel(IProfileAccessService access,TopstarDbContext db):BrandGuardedPageModel(access)
{
 [BindProperty(SupportsGet=true)]public Guid Id{get;set;}
 [BindProperty]public CampaignEditModel.ExecutionInputModel Input{get;set;}=new();
 public string CampaignTitle{get;private set;}="";
 public bool IsOnsite{get;private set;}
 public bool CanEdit{get;private set;}
 [TempData]public string? Message{get;set;}
 private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private Task<CampaignEntity?> Find(CancellationToken ct)=> (from c in db.Campaigns join b in db.BrandProfiles on c.BrandProfileId equals b.Id where c.Id==Id&&b.UserId==Actor select c).SingleOrDefaultAsync(ct);
 private async Task Display(CampaignEntity c,CancellationToken ct){CampaignTitle=c.Title;IsOnsite=CampaignExecutionModes.IsOnsite(c.ExecutionMode);CanEdit=c.Status is not ("InProgress" or "Completed" or "Cancelled" or "Rejected")&&!await db.Participations.AnyAsync(p=>p.CampaignId==Id&&p.WorkOpenedAtUtc!=null,ct);}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;var c=await Find(ct);if(c is null)return NotFound();await Display(c,ct);Input=new(){Mode=c.ExecutionMode,VenueName=c.VenueName,VenueAddress=c.VenueAddress,ContactName=c.ContactName,ContactPhone=c.ContactPhone,Schedule=c.ExecutionSchedule,BookingInstructions=c.BookingInstructions,ShippingInstructions=c.ShippingInstructions,SupportDetails=c.SupportDetails,Requirements=c.ExecutionRequirements};return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){var guard=await RedirectIfProfileIncompleteAsync(false);if(guard is not null)return guard;
 await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
 var c=await Find(ct);if(c is null)return NotFound();await Display(c,ct);if(!CanEdit)return Forbid();
 Input.Mode=c.ExecutionMode;ModelState.Remove("Input.Mode");
 if(!string.IsNullOrWhiteSpace(Input.ContactPhone)&&!System.Text.RegularExpressions.Regex.IsMatch(Input.ContactPhone.Trim(),@"^[0-9+\s().-]{8,20}$"))ModelState.AddModelError("Input.ContactPhone","Số điện thoại liên hệ không đúng định dạng.");
 if(!ModelState.IsValid)return Page();
 string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
 var before=new{c.VenueName,c.VenueAddress,c.ContactName,c.ContactPhone,c.ExecutionSchedule,c.BookingInstructions,c.ShippingInstructions,c.SupportDetails,c.ExecutionRequirements};
 if(IsOnsite){c.VenueName=Clean(Input.VenueName);c.VenueAddress=Clean(Input.VenueAddress);c.ContactName=Clean(Input.ContactName);c.ContactPhone=Clean(Input.ContactPhone);c.BookingInstructions=Clean(Input.BookingInstructions);}
 c.ExecutionSchedule=Clean(Input.Schedule);if(c.ExecutionMode==CampaignExecutionModes.RemoteProductReview)c.ShippingInstructions=Clean(Input.ShippingInstructions);
 c.SupportDetails=Clean(Input.SupportDetails);c.ExecutionRequirements=Clean(Input.Requirements);c.UpdatedAtUtc=DateTime.UtcNow;
 db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="Campaign",EntityId=Id.ToString(),ActionName="UpdatePreparation",PayloadJson=JsonSerializer.Serialize(new{Before=before,After=Input}),CreatedAtUtc=DateTime.UtcNow});
 await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu thông tin triển khai.";return RedirectToPage(new{Id});}
}
