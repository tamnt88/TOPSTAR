using Microsoft.EntityFrameworkCore;
using Topstar.Application.Notifications;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Notifications;
public sealed class NotificationTemplateAdminService(TopstarDbContext db):INotificationTemplateAdminService
{
 public async Task<IReadOnlyList<NotificationTemplateItem>> GetAllAsync(CancellationToken ct=default)
 {
  var items=await db.NotificationTemplates.AsNoTracking().OrderBy(x=>x.Type).ThenBy(x=>x.AudienceRole).ToListAsync(ct);var ids=items.Select(x=>x.Id).ToArray();
  var history=await(from h in db.NotificationTemplateHistories.AsNoTracking() join u in db.Users.AsNoTracking() on h.ActorUserId equals u.Id where ids.Contains(h.TemplateId) orderby h.CreatedAtUtc select new{h,u.Email}).ToListAsync(ct);
  return items.Select(x=>new NotificationTemplateItem(x.Id,x.Type,x.AudienceRole,x.TitleTemplate,x.BodyTemplate,x.IsActive,history.Where(h=>h.h.TemplateId==x.Id).Select(h=>new NotificationTemplateHistoryItem(h.h.TitleTemplate,h.h.BodyTemplate,h.h.IsActive,h.h.Reason,h.Email,h.h.CreatedAtUtc)).ToList())).ToList();
 }
 public async Task<NotificationTemplateResult> SaveAsync(Guid actor,Guid? id,string type,string role,string title,string body,bool active,string reason,CancellationToken ct=default)
 {
  type=type?.Trim()??"";role=role?.Trim().ToUpperInvariant()??"";title=title?.Trim()??"";body=body?.Trim()??"";reason=reason?.Trim()??"";
  if(type.Length is <2 or >50||!NotificationTemplateRules.IsValidRole(role)||title.Length is <1 or >200||body.Length is <1 or >1000||reason.Length is <5 or >1000)return new(false,"Kiểm tra type, role, template và lý do 5–1.000 ký tự.");
  if(!NotificationTemplateRules.HasOnlySupportedPlaceholders(title)||!NotificationTemplateRules.HasOnlySupportedPlaceholders(body))return new(false,"Chỉ hỗ trợ placeholder {Title} và {Body}.");
  var now=DateTime.UtcNow;var item=id.HasValue?await db.NotificationTemplates.FirstOrDefaultAsync(x=>x.Id==id,ct):null;
  if(id.HasValue&&item is null)return new(false,"Không tìm thấy template.");
  if(item is null){if(await db.NotificationTemplates.AnyAsync(x=>x.Type==type&&x.AudienceRole==role,ct))return new(false,"Type và role đã tồn tại.");item=new(){Id=Guid.NewGuid(),Type=type,AudienceRole=role,CreatedByUserId=actor,CreatedAtUtc=now};db.NotificationTemplates.Add(item);}
  else if(item.Type!=type||item.AudienceRole!=role)return new(false,"Type và audience role là bất biến.");
  item.TitleTemplate=title;item.BodyTemplate=body;item.IsActive=active;item.UpdatedByUserId=actor;item.UpdatedAtUtc=now;
  db.NotificationTemplateHistories.Add(new(){Id=Guid.NewGuid(),TemplateId=item.Id,TitleTemplate=title,BodyTemplate=body,IsActive=active,Reason=reason,ActorUserId=actor,CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);return new(true,"Đã lưu notification template và version history.");
 }
}
