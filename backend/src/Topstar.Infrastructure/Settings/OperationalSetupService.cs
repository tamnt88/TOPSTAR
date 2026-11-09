using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Settings;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Settings;

public sealed class OperationalSetupService(TopstarDbContext db) : IOperationalSetupService
{
    public async Task<OperationalPolicyItem> GetPolicyAsync(CancellationToken ct=default)
    {
        var p=await db.OperationalRuntimePolicies.AsNoTracking().SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Chưa có cấu hình vận hành. Vui lòng chạy SQL setup được bàn giao.");
        var history=await (from h in db.OperationalRuntimePolicyHistories.AsNoTracking()
            join u in db.Users.AsNoTracking() on h.ActorUserId equals u.Id
            where h.PolicyId==p.Id orderby h.CreatedAtUtc descending
            select new OperationalPolicyHistoryItem(h.SnapshotJson,h.Reason,u.Email,h.CreatedAtUtc)).Take(30).ToListAsync(ct);
        return new(p.ReviewEditHours,p.BookingLeadMinutes,p.BookingMaxHours,p.MaxPayoutAccounts,p.OtpRequestLimit,
            p.OtpWindowMinutes,p.OtpExpiryMinutes,p.OtpMaxAttempts,p.UpdatedAtUtc,history);
    }

    public async Task<OperationalSetupResult> UpdatePolicyAsync(Guid actorId,int reviewEditHours,int bookingLeadMinutes,
        int bookingMaxHours,int maxPayoutAccounts,int otpRequestLimit,int otpWindowMinutes,int otpExpiryMinutes,
        int otpMaxAttempts,string reason,CancellationToken ct=default)
    {
        reason=(reason??string.Empty).Trim();
        if(reviewEditHours is <1 or >168||bookingLeadMinutes is <1 or >1440||bookingMaxHours is <1 or >168||
           maxPayoutAccounts is <1 or >20||otpRequestLimit is <1 or >20||otpWindowMinutes is <1 or >1440||
           otpExpiryMinutes is <1 or >60||otpMaxAttempts is <1 or >20||reason.Length is <10 or >500)
            return new(false,"Thông số hoặc lý do thay đổi không hợp lệ.");
        var p=await db.OperationalRuntimePolicies.SingleOrDefaultAsync(ct);
        if(p is null)return new(false,"Chưa có cấu hình vận hành. Vui lòng chạy SQL setup.");
        var before=Snapshot(p);var now=DateTime.UtcNow;
        p.ReviewEditHours=reviewEditHours;p.BookingLeadMinutes=bookingLeadMinutes;p.BookingMaxHours=bookingMaxHours;
        p.MaxPayoutAccounts=maxPayoutAccounts;p.OtpRequestLimit=otpRequestLimit;p.OtpWindowMinutes=otpWindowMinutes;
        p.OtpExpiryMinutes=otpExpiryMinutes;p.OtpMaxAttempts=otpMaxAttempts;p.UpdatedByUserId=actorId;p.UpdatedAtUtc=now;
        db.OperationalRuntimePolicyHistories.Add(new(){Id=Guid.NewGuid(),PolicyId=p.Id,SnapshotJson=before,Reason=reason,ActorUserId=actorId,CreatedAtUtc=now});
        Audit(actorId,p.Id,"UpdateOperationalPolicy",new{Before=before,After=Snapshot(p),Reason=reason},now);
        await db.SaveChangesAsync(ct);return new(true,"Đã cập nhật cấu hình vận hành.",p.Id);
    }

    public async Task<IReadOnlyList<OperationalCatalogItem>> GetCatalogAsync(string? type=null,bool activeOnly=false,CancellationToken ct=default)
        => await db.OperationalCatalogItems.AsNoTracking().Where(x=>(type==null||x.CatalogType==type)&&(!activeOnly||x.IsActive))
            .OrderBy(x=>x.CatalogType).ThenBy(x=>x.SortOrder).ThenBy(x=>x.Name)
            .Select(x=>new OperationalCatalogItem(x.Id,x.CatalogType,x.Code,x.Name,x.Description,x.DefaultSeverity,x.DefaultPenaltyPoints,x.SortOrder,x.IsActive,x.UpdatedAtUtc)).ToListAsync(ct);
    public async Task<OperationalCatalogItem?> FindCatalogAsync(Guid id,CancellationToken ct=default)
        => await db.OperationalCatalogItems.AsNoTracking().Where(x=>x.Id==id).Select(x=>new OperationalCatalogItem(x.Id,x.CatalogType,x.Code,x.Name,x.Description,x.DefaultSeverity,x.DefaultPenaltyPoints,x.SortOrder,x.IsActive,x.UpdatedAtUtc)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<OperationalCatalogHistoryItem>> GetCatalogHistoryAsync(CancellationToken ct=default)
        =>await(from h in db.OperationalCatalogHistories.AsNoTracking() join u in db.Users.AsNoTracking() on h.ActorUserId equals u.Id orderby h.CreatedAtUtc descending select new OperationalCatalogHistoryItem(h.Action,h.SnapshotJson,h.Reason,u.Email,h.CreatedAtUtc)).Take(100).ToListAsync(ct);

    public async Task<OperationalSetupResult> SaveCatalogAsync(Guid actorId,Guid? id,string type,string code,string name,string? description,string? severity,int? points,int sortOrder,bool isActive,string reason,CancellationToken ct=default)
    {
        type=(type??"").Trim().ToUpperInvariant();code=(code??"").Trim().ToLowerInvariant();name=(name??"").Trim();description=description?.Trim();severity=severity?.Trim();reason=(reason??"").Trim();
        if(!OperationalCatalogTypes.All.Contains(type)||code.Length is <2 or >80||!code.All(c=>char.IsLetterOrDigit(c)||c is '-' or '_')||name.Length is <2 or >150||description?.Length>500||sortOrder is <0 or >99999||reason.Length is <10 or >500)
            return new(false,"Dữ liệu danh mục hoặc lý do thay đổi không hợp lệ.");
        if(type==OperationalCatalogTypes.Violation&&((severity is not ("Low" or "Medium" or "High"))||points is <0 or >100))return new(false,"Danh mục vi phạm cần mức độ và điểm phạt hợp lệ.");
        if(type!=OperationalCatalogTypes.Violation){severity=null;points=null;}
        if(await db.OperationalCatalogItems.AnyAsync(x=>x.CatalogType==type&&x.Code==code&&x.Id!=id,ct))return new(false,"Mã đã tồn tại trong nhóm danh mục.");
        var now=DateTime.UtcNow;var e=id.HasValue?await db.OperationalCatalogItems.SingleOrDefaultAsync(x=>x.Id==id,ct):null;
        if(id.HasValue&&e is null)return new(false,"Không tìm thấy danh mục.");
        var action=e is null?"Create":"Update";
        if(e is null){e=new(){Id=Guid.NewGuid(),CreatedAtUtc=now};db.OperationalCatalogItems.Add(e);}
        e.CatalogType=type;e.Code=code;e.Name=name;e.Description=description;e.DefaultSeverity=severity;e.DefaultPenaltyPoints=points;e.SortOrder=sortOrder;e.IsActive=isActive;e.UpdatedAtUtc=now;
        var snap=JsonSerializer.Serialize(new{e.CatalogType,e.Code,e.Name,e.Description,e.DefaultSeverity,e.DefaultPenaltyPoints,e.SortOrder,e.IsActive});
        db.OperationalCatalogHistories.Add(new(){Id=Guid.NewGuid(),CatalogItemId=e.Id,Action=action,SnapshotJson=snap,Reason=reason,ActorUserId=actorId,CreatedAtUtc=now});
        Audit(actorId,e.Id,action+"OperationalCatalog",new{Snapshot=snap,Reason=reason},now);await db.SaveChangesAsync(ct);
        return new(true,action=="Create"?"Đã thêm danh mục.":"Đã cập nhật danh mục.",e.Id);
    }
    public async Task<OperationalSetupResult> DeleteCatalogAsync(Guid actorId,Guid id,string reason,CancellationToken ct=default)
    {
        reason=(reason??"").Trim();if(reason.Length is <10 or >500)return new(false,"Lý do xóa phải từ 10 đến 500 ký tự.");
        var e=await db.OperationalCatalogItems.SingleOrDefaultAsync(x=>x.Id==id,ct);if(e is null)return new(false,"Không tìm thấy danh mục.");
        var used=e.CatalogType switch{OperationalCatalogTypes.Deliverable=>await db.CampaignDeliverables.AnyAsync(x=>x.DeliverableType==e.Code,ct),OperationalCatalogTypes.Violation=>await db.UserViolations.AnyAsync(x=>x.Category==e.Code,ct),_=>await db.ShipmentTrackingEvents.AnyAsync(x=>x.EventCode==e.Code,ct)};
        if(used)return new(false,"Danh mục đã phát sinh dữ liệu; hãy tạm ẩn thay vì xóa.");
        var now=DateTime.UtcNow;var snap=JsonSerializer.Serialize(e);db.OperationalCatalogHistories.Add(new(){Id=Guid.NewGuid(),CatalogItemId=e.Id,Action="Delete",SnapshotJson=snap,Reason=reason,ActorUserId=actorId,CreatedAtUtc=now});Audit(actorId,e.Id,"DeleteOperationalCatalog",new{Snapshot=snap,Reason=reason},now);db.OperationalCatalogItems.Remove(e);await db.SaveChangesAsync(ct);return new(true,"Đã xóa danh mục chưa phát sinh dữ liệu.");
    }
    private static string Snapshot(OperationalRuntimePolicyEntity p)=>JsonSerializer.Serialize(new{p.ReviewEditHours,p.BookingLeadMinutes,p.BookingMaxHours,p.MaxPayoutAccounts,p.OtpRequestLimit,p.OtpWindowMinutes,p.OtpExpiryMinutes,p.OtpMaxAttempts});
    private void Audit(Guid actor,Guid id,string action,object data,DateTime now)=>db.AuditLogs.Add(new(){ActorUserId=actor,ActionName=action,EntityName="OperationalSetup",EntityId=id.ToString(),PayloadJson=JsonSerializer.Serialize(data),CreatedAtUtc=now});
}
