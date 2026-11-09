using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Finance;
public static class CampaignCreationPayment {
 public static async Task ApplyAsync(TopstarDbContext db,CancellationToken ct){
  var ids=db.ChangeTracker.Entries<CampaignFundingEntity>().Where(e=>e.State==EntityState.Modified&&e.Entity.Status=="Paid"&&e.Property(x=>x.Status).OriginalValue!="Paid").Select(e=>e.Entity.CampaignId).Distinct().ToList();
  foreach(var id in ids){
   var c=await db.Campaigns.SingleAsync(x=>x.Id==id,ct);
   if(!c.CheckoutAtCreation||c.Status!="Draft")continue;
   var now=DateTime.UtcNow;c.Status="Submitted";c.IsVisible=false;c.UpdatedAtUtc=now;
   db.CampaignStatusHistories.Add(new(){Id=Guid.NewGuid(),CampaignId=id,FromStatus="Draft",ToStatus="Submitted",Note="Đã thanh toán, chờ duyệt nội dung chiến dịch.",CreatedAtUtc=now});
   var owner=await db.BrandProfiles.Where(b=>b.Id==c.BrandProfileId).Select(b=>b.UserId).SingleAsync(ct);
   db.Notifications.Add(new(){Id=Guid.NewGuid(),UserId=owner,Type="Campaign",Title="Đã thanh toán chiến dịch",Body="Chiến dịch "+c.Title+" đang chờ duyệt nội dung.",SourceEntityType="Campaign",SourceEntityId=id,AudienceRole="BRAND",IsVisible=true,TargetUrl="/Brand/CampaignWorkspace?id="+id,CreatedAtUtc=now});
  }
 }
}
