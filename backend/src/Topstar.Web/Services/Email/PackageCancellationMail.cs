using System.Net;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Services.Email;
public static class PackageCancellationMail
{
 public static async Task SendAsync(HttpContext context,Guid id,string message,CancellationToken ct)
 {
  var services=context.RequestServices;
  var logger=services.GetRequiredService<ILoggerFactory>().CreateLogger("PackageCancellationMail");
  try{
   var db=services.GetRequiredService<TopstarDbContext>();
   var item=await (from p in db.BrandPackagePurchases.AsNoTracking() join b in db.BrandProfiles on p.BrandProfileId equals b.Id join u in db.Users on b.UserId equals u.Id where p.Id==id select new{p.OrderNumber,p.PackageNameSnapshot,p.AmountSnapshot,u.Email}).SingleAsync(ct);
   var recipients=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id join u in db.Users on ur.UserId equals u.Id where r.Code=="Admin"||r.Code=="ADMIN" select u.Email).ToListAsync(ct);
   recipients.Add(item.Email);
   var html=services.GetRequiredService<IEmailTemplateRenderer>().Render(new(){MainTitle="Cập nhật yêu cầu hủy gói",Preheader=message,BodyHtml=$"<p>{WebUtility.HtmlEncode(message)}</p><p><strong>{WebUtility.HtmlEncode(item.PackageNameSnapshot)}</strong></p><p>Mã đơn: {WebUtility.HtmlEncode(item.OrderNumber)}</p><p>Giá trị gói: {item.AmountSnapshot:N0} đ</p><p>Đăng nhập hệ thống để xem trạng thái và lịch sử xử lý.</p>"});
   foreach(var email in recipients.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct())
    if(!await services.GetRequiredService<IEmailSender>().SendAsync(email!,"Cập nhật hủy gói - "+item.OrderNumber,html,ct))logger.LogWarning("Email cancellation not delivered for purchase {PurchaseId}",id);
  }catch(Exception ex){logger.LogError(ex,"Failed to send cancellation email for purchase {PurchaseId}",id);}
 }
}
