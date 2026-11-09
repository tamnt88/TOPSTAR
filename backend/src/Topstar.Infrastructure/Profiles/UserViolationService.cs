using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;
using Topstar.Application.Settings;

namespace Topstar.Infrastructure.Profiles;

public sealed class UserViolationService(TopstarDbContext db):IUserViolationService
{
    public async Task<IReadOnlyList<UserViolationItem>> GetAsync(string? status,Guid? userId=null,CancellationToken ct=default)
    {
        status=status?.Trim() is "Active" or "Revoked"?status.Trim():null;
        var items=await(from v in db.UserViolations.AsNoTracking()
            join u in db.Users.AsNoTracking() on v.UserId equals u.Id
            let brand=db.BrandProfiles.AsNoTracking().Where(x=>x.UserId==u.Id).Select(x=>x.CompanyName).FirstOrDefault()
            let koc=db.KocProfiles.AsNoTracking().Where(x=>x.UserId==u.Id).Select(x=>x.DisplayName).FirstOrDefault()
            where (status==null||v.Status==status)&&(!userId.HasValue||v.UserId==userId.Value)
            orderby v.Status=="Active"?0:1,v.CreatedAtUtc descending
            select new UserViolationItem(v.Id,u.Id,brand??koc??u.Email,u.Email,brand!=null?"Brand":"KOC",v.Category,v.Severity,v.PenaltyPoints,v.Reason,v.Status,v.ResolutionNote,v.CreatedAtUtc))
            .Take(300).ToListAsync(ct);
        var ids=items.Select(x=>x.Id).ToArray();var histories=await db.UserViolationHistories.AsNoTracking().Where(x=>ids.Contains(x.ViolationId)).OrderBy(x=>x.CreatedAtUtc).ToListAsync(ct);
        return items.Select(x=>x with{History=histories.Where(h=>h.ViolationId==x.Id).Select(h=>new UserViolationEvent(h.FromStatus,h.ToStatus,h.Note,h.CreatedAtUtc)).ToList()}).ToList();
    }

    public async Task<UserViolationResult> CreateAsync(Guid admin,string email,string category,string severity,int penalty,string reason,CancellationToken ct=default)
    {
        email=email?.Trim().ToLowerInvariant()??"";category=category?.Trim()??"";severity=severity?.Trim()??"";reason=reason?.Trim()??"";
        var configured=await db.OperationalCatalogItems.AsNoTracking().SingleOrDefaultAsync(x=>x.CatalogType==OperationalCatalogTypes.Violation&&x.Code==category&&x.IsActive,ct);
        if(configured is null)return Fail("Loại vi phạm không có trong cấu hình hoặc đang tạm ẩn.");
        severity=configured.DefaultSeverity??severity;penalty=configured.DefaultPenaltyPoints??penalty;
        if(category.Length is <2 or >100||reason.Length is <10 or >2000||!UserViolationRules.IsValidSeverity(severity)||!UserViolationRules.IsValidPenalty(penalty))return Fail("Kiểm tra category, severity, penalty 0–100 và lý do 10–2.000 ký tự.");
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Email==email,ct);if(user is null)return Fail("Không tìm thấy tài khoản.");
        var isBrand=await db.BrandProfiles.AsNoTracking().AnyAsync(x=>x.UserId==user.Id,ct);var isKoc=await db.KocProfiles.AsNoTracking().AnyAsync(x=>x.UserId==user.Id,ct);if(!isBrand&&!isKoc)return Fail("Chỉ ghi nhận vi phạm cho Brand hoặc KOC.");
        var now=DateTime.UtcNow;var item=new UserViolationEntity{Id=Guid.NewGuid(),UserId=user.Id,Category=category,Severity=severity,PenaltyPoints=penalty,Reason=reason,Status="Active",CreatedByUserId=admin,CreatedAtUtc=now};
        db.UserViolations.Add(item);db.UserViolationHistories.Add(new(){Id=Guid.NewGuid(),ViolationId=item.Id,FromStatus=null,ToStatus="Active",Note=reason,ActorUserId=admin,CreatedAtUtc=now});
        db.Notifications.Add(NotificationFactory.Create(user.Id,"Violation","Tài khoản có cảnh báo vận hành",$"{category}: {reason}",now,"UserViolation",item.Id,audienceRole:isBrand?"BRAND":"KOC"));
        db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="UserViolation",EntityId=item.Id.ToString(),ActionName="Create",PayloadJson=JsonSerializer.Serialize(new{user.Id,category,severity,penalty}),CreatedAtUtc=now});
        await db.SaveChangesAsync(ct);return Ok("Đã ghi nhận vi phạm và lưu lịch sử.");
    }

    public async Task<UserViolationResult> RevokeAsync(Guid admin,Guid id,string reason,CancellationToken ct=default)
    {
        reason=reason?.Trim()??"";if(reason.Length is <10 or >1000)return Fail("Lý do thu hồi phải từ 10–1.000 ký tự.");
        var item=await db.UserViolations.FirstOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return Fail("Không tìm thấy vi phạm.");if(item.Status!="Active")return Fail("Vi phạm không còn hiệu lực.");
        var now=DateTime.UtcNow;item.Status="Revoked";item.ResolutionNote=reason;item.RevokedByUserId=admin;item.RevokedAtUtc=now;item.UpdatedAtUtc=now;
        db.UserViolationHistories.Add(new(){Id=Guid.NewGuid(),ViolationId=id,FromStatus="Active",ToStatus="Revoked",Note=reason,ActorUserId=admin,CreatedAtUtc=now});
        db.Notifications.Add(NotificationFactory.Create(item.UserId,"Violation","Cảnh báo đã được thu hồi",reason,now,"UserViolation",id));
        db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="UserViolation",EntityId=id.ToString(),ActionName="Revoke",PayloadJson=JsonSerializer.Serialize(new{reason}),CreatedAtUtc=now});
        await db.SaveChangesAsync(ct);return Ok("Đã thu hồi vi phạm; penalty không còn ảnh hưởng.");
    }
    private static UserViolationResult Ok(string m)=>new(true,m);private static UserViolationResult Fail(string m)=>new(false,m);
}
