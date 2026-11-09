using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Payments;
namespace Topstar.Web.ViewComponents;
public sealed class KocTaxSummaryViewComponent(TopstarDbContext db):ViewComponent{
 public async Task<IViewComponentResult> InvokeAsync(Guid participationId,decimal? amount=null){
  if(!Guid.TryParse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),out var user))return Content("");
  var allowed=HttpContext.User.IsInRole("Admin")||await (from p in db.Participations join k in db.KocProfiles on p.KocProfileId equals k.Id join c in db.Campaigns on p.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where p.Id==participationId&&(k.UserId==user||b.UserId==user) select p.Id).AnyAsync(HttpContext.RequestAborted);
  if(!allowed)return Content("");var data=await KocTaxPresentation.ReadAsync(db,participationId,amount,HttpContext.RequestAborted);return data is null?Content(""):View(data);
 }
}
