using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Infrastructure.Profiles;
public sealed class UserAccountAdminService(TopstarDbContext db):IUserAccountAdminService
{
 public async Task<UserAccountAdminDetail?> GetDetailAsync(Guid userId,CancellationToken ct=default)
 {
  var user=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null)return null;
  var brand=await db.BrandProfiles.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==userId,ct);
  var koc=await db.KocProfiles.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==userId,ct);
  var type=brand is not null?"Brand":"KOC";var name=brand?.CompanyName??koc?.DisplayName??user.Email;var profileStatus=brand?.Status??koc?.Status??"—";
  var sessions=await(from s in db.AuthSessions.AsNoTracking()
   join d in db.UserDevices.AsNoTracking() on s.UserDeviceId equals d.Id into devices from d in devices.DefaultIfEmpty()
   where s.UserId==userId orderby s.IssuedAtUtc descending
   select new UserSessionAdminItem(s.Id,d==null?"Thiết bị không xác định":(d.DeviceName??d.BrowserName??d.DeviceType??"Thiết bị"),d==null?null:d.IpAddress,d==null?null:d.UserAgent,s.IssuedAtUtc,s.ExpiresAtUtc,s.RevokedAtUtc,s.RevokeReason)).Take(30).ToListAsync(ct);
  var events=await db.AuthEvents.AsNoTracking().Where(x=>x.UserId==userId).OrderByDescending(x=>x.OccurredAtUtc)
   .Select(x=>new UserAuthEventAdminItem(x.Id,x.EventType,x.EventStatus,x.FailureReason,x.IpAddress,x.UserAgent,x.OccurredAtUtc)).Take(50).ToListAsync(ct);
  var id=userId.ToString();var audits=await db.AuditLogs.AsNoTracking().Where(x=>x.EntityName=="User"&&x.EntityId==id).OrderByDescending(x=>x.CreatedAtUtc)
   .Select(x=>new UserAuditAdminItem(x.Id,x.ActionName,x.ActorEmailSnapshot,x.ActorRoleSnapshot,x.PayloadJson,x.CreatedAtUtc)).Take(50).ToListAsync(ct);
  var notes=await(from n in db.UserAccountAdminNotes.AsNoTracking() join a in db.Users.AsNoTracking() on n.ActorUserId equals a.Id into actors from a in actors.DefaultIfEmpty()
   where n.UserId==userId orderby n.CreatedAtUtc descending select new UserAdminNoteItem(n.Id,n.Body,n.ActorUserId,a==null?null:a.Email,n.CreatedAtUtc)).Take(50).ToListAsync(ct);
  return new(user.Id,type,name,user.Email,user.Phone,user.Status,profileStatus,user.EmailConfirmed,user.PhoneConfirmed,user.CreatedAtUtc,user.LastLoginAtUtc,sessions,events,audits,notes);
 }

 public async Task<UserAccountAdminResult> SetVerificationAsync(Guid admin,Guid userId,bool? emailConfirmed,bool? phoneConfirmed,string reason,CancellationToken ct=default)
 {
  reason=CleanReason(reason);if(reason.Length<10)return Fail("Lý do phải có ít nhất 10 ký tự.");
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null)return Fail("Không tìm thấy tài khoản.");
  var before=new{user.EmailConfirmed,user.PhoneConfirmed};if(emailConfirmed.HasValue)user.EmailConfirmed=emailConfirmed.Value;if(phoneConfirmed.HasValue)user.PhoneConfirmed=phoneConfirmed.Value;
  user.UpdatedAtUtc=DateTime.UtcNow;db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName="SetVerification",PayloadJson=JsonSerializer.Serialize(new{Before=before,After=new{user.EmailConfirmed,user.PhoneConfirmed},Reason=reason}),CreatedAtUtc=DateTime.UtcNow});
  await db.SaveChangesAsync(ct);return new(true,"Đã cập nhật trạng thái xác minh và lưu audit.");
 }
 public async Task<UserAccountAdminResult> RevokeSessionAsync(Guid admin,Guid userId,Guid? sessionId,string reason,CancellationToken ct=default)
 {
  reason=CleanReason(reason);if(reason.Length<10)return Fail("Lý do phải có ít nhất 10 ký tự.");
  var query=db.AuthSessions.Where(x=>x.UserId==userId&&!x.RevokedAtUtc.HasValue);if(sessionId.HasValue)query=query.Where(x=>x.Id==sessionId.Value);
  var rows=await query.ToListAsync(ct);var now=DateTime.UtcNow;foreach(var row in rows){row.RevokedAtUtc=now;row.RevokeReason=reason;row.UpdatedAtUtc=now;}
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName=sessionId.HasValue?"RevokeSession":"RevokeAllSessions",PayloadJson=JsonSerializer.Serialize(new{SessionId=sessionId,Count=rows.Count,Reason=reason}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);return new(true,$"Đã thu hồi {rows.Count} phiên đăng nhập.");
 }
 public async Task<UserAccountAdminResult> AddNoteAsync(Guid admin,Guid userId,string body,CancellationToken ct=default)
 {
  body=body?.Trim()??"";if(body.Length is <3 or >2000)return Fail("Ghi chú phải từ 3–2.000 ký tự.");
  if(!await db.Users.AnyAsync(x=>x.Id==userId,ct))return Fail("Không tìm thấy tài khoản.");
  var now=DateTime.UtcNow;db.UserAccountAdminNotes.Add(new(){Id=Guid.NewGuid(),UserId=userId,ActorUserId=admin,Body=body,CreatedAtUtc=now});
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName="AddInternalNote",PayloadJson=JsonSerializer.Serialize(new{Length=body.Length}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);return new(true,"Đã lưu ghi chú nội bộ.");
 }
 public async Task<UserAccountAdminResult> RecordSecurityActionAsync(Guid admin,Guid userId,string action,string reason,CancellationToken ct=default)
 {
  reason=CleanReason(reason);if(reason.Length<10)return Fail("Lý do phải có ít nhất 10 ký tự.");
  if(!await db.Users.AnyAsync(x=>x.Id==userId,ct))return Fail("Không tìm thấy tài khoản.");
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName=action,PayloadJson=JsonSerializer.Serialize(new{Reason=reason}),CreatedAtUtc=DateTime.UtcNow});
  await db.SaveChangesAsync(ct);return new(true,"Đã ghi nhận thao tác bảo mật.");
 }
 public async Task<UserAccountAdminResult> RecordCreateAsync(Guid admin,Guid userId,string profileType,string reason,CancellationToken ct=default)
 {
  reason=reason?.Trim()??"";if(reason.Length is <10 or >1000)return Fail("Lý do tạo phải từ 10–1.000 ký tự.");
  var user=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null)return Fail("Không tìm thấy tài khoản vừa tạo.");
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName="Create",PayloadJson=JsonSerializer.Serialize(new{ProfileType=profileType,user.Email,Reason=reason}),CreatedAtUtc=DateTime.UtcNow});
  await db.SaveChangesAsync(ct);return new(true,"Đã tạo tài khoản và lưu audit.");
 }

 public async Task<UserAccountAdminResult> UpdateDirectoryAsync(Guid admin,Guid userId,string profileType,string name,string email,string? location,string reason,CancellationToken ct=default)
 {
  name=name?.Trim()??"";email=email?.Trim().ToLowerInvariant()??"";location=string.IsNullOrWhiteSpace(location)?null:location.Trim();reason=reason?.Trim()??"";
  if(name.Length is <2 or >200)return Fail("Tên hiển thị phải từ 2–200 ký tự.");
  if(!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)||email.Length>255)return Fail("Email không hợp lệ.");
  if(location?.Length>200)return Fail("Khu vực/ngành hàng tối đa 200 ký tự.");
  if(reason.Length is <10 or >1000)return Fail("Lý do cập nhật phải từ 10–1.000 ký tự.");
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  _=await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r INT,@resource NVARCHAR(255)=CONCAT(N'UserAccount:',CONVERT(nvarchar(36),{userId}));EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;IF @r<0 THROW 51000,N'Không thể khóa giao dịch tài khoản.',1;",ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null)return Fail("Không tìm thấy tài khoản.");
  if(await db.Users.AsNoTracking().AnyAsync(x=>x.Id!=userId&&x.Email==email,ct))return Fail("Email đã được tài khoản khác sử dụng.");
  var now=DateTime.UtcNow;var beforeEmail=user.Email;user.Email=email;user.UpdatedAtUtc=now;
  object before;object after;
  if(string.Equals(profileType,"Brand",StringComparison.OrdinalIgnoreCase))
  {
   var profile=await db.BrandProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);if(profile is null)return Fail("Không tìm thấy hồ sơ Brand.");
   before=new{Email=beforeEmail,Name=profile.CompanyName,Location=profile.Industry};
   profile.CompanyName=name;profile.Industry=location;profile.UpdatedAtUtc=now;after=new{Email=email,Name=name,Location=location};
  }
  else
  {
   var profile=await db.KocProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);if(profile is null)return Fail("Không tìm thấy hồ sơ KOC.");
   before=new{Email=beforeEmail,Name=profile.DisplayName,Location=profile.Region};
   profile.DisplayName=name;profile.Region=location;profile.UpdatedAtUtc=now;after=new{Email=email,Name=name,Location=location};
  }
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName="Update",PayloadJson=JsonSerializer.Serialize(new{ProfileType=profileType,Before=before,After=after,Reason=reason}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,"Đã cập nhật tài khoản và lưu audit.");
 }

 public async Task<UserAccountAdminResult> ChangeLockAsync(Guid admin,Guid userId,bool locked,string reason,CancellationToken ct=default)
 {
  reason=reason?.Trim()??"";if(reason.Length is <10 or >1000)return Fail("Lý do phải từ 10–1.000 ký tự.");
  await db.Database.ExecuteSqlRawAsync("SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON;",ct);
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  _=await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r INT,@resource NVARCHAR(255)=CONCAT(N'UserAccount:',CONVERT(nvarchar(36),{userId}));EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;IF @r<0 THROW 51000,N'Không thể khóa giao dịch tài khoản.',1;",ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null)return Fail("Không tìm thấy tài khoản.");
  var brand=await db.BrandProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);var koc=await db.KocProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);
  if(brand is null&&koc is null)return Fail("Chỉ quản lý khóa tài khoản Brand hoặc KOC.");
  var profileType=brand is not null?"Brand":"KOC";var profileStatus=brand?.Status??koc!.Status;
  if(!UserAccountAdminRules.CanChange(locked,user.Status,profileStatus))return Fail(locked?"Chỉ khóa tài khoản Active có hồ sơ Approved.":"Chỉ mở khóa tài khoản đang Locked.");
  var fromUser=user.Status;var toUser=locked?"Locked":"Active";var toProfile=profileStatus;var now=DateTime.UtcNow;
  user.Status=toUser;user.UpdatedAtUtc=now;
  if(locked){var sessions=await db.AuthSessions.Where(x=>x.UserId==userId&&!x.RevokedAtUtc.HasValue).ToListAsync(ct);foreach(var session in sessions){session.RevokedAtUtc=now;session.RevokeReason=reason;session.UpdatedAtUtc=now;}}
  db.UserAccountStatusHistories.Add(new(){Id=Guid.NewGuid(),UserId=userId,FromStatus=fromUser,ToStatus=toUser,ProfileType=profileType,FromProfileStatus=profileStatus,ToProfileStatus=toProfile,Reason=reason,ActorUserId=admin,CreatedAtUtc=now});
  db.Notifications.Add(NotificationFactory.Create(userId,"AccountStatus",locked?"Tài khoản đã bị khóa":"Tài khoản đã được mở khóa",reason,now,"User",userId,audienceRole:profileType.ToUpperInvariant()));
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName=locked?"Lock":"Unlock",PayloadJson=JsonSerializer.Serialize(new{profileType,fromUser,toUser,ProfileStatus=profileStatus,reason}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(true,locked?"Đã khóa tài khoản và thu hồi session.":"Đã mở khóa tài khoản.");
 }
 public async Task<UserAccountAdminResult> RemoveAsync(Guid admin,Guid userId,string reason,CancellationToken ct=default)
 {
  reason=CleanReason(reason);if(reason.Length<10)return Fail("Lý do xóa phải có ít nhất 10 ký tự.");
  if(admin==userId)return Fail("Không thể tự xóa tài khoản Admin đang đăng nhập.");
  await using var tx=await db.Database.BeginTransactionAsync(ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId,ct);if(user is null||user.Status=="Disabled")return Fail("Không tìm thấy tài khoản hoặc tài khoản đã được xóa.");
  var brand=await db.BrandProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);
  var koc=await db.KocProfiles.FirstOrDefaultAsync(x=>x.UserId==userId,ct);
  if(brand is null&&koc is null)return Fail("Chỉ có thể xóa tài khoản KOC hoặc Brand.");
  var now=DateTime.UtcNow;var fromStatus=user.Status;var profileType=brand is not null?"Brand":"KOC";var fromProfileStatus=brand?.Status??koc!.Status;
  user.Status="Disabled";user.LockoutEndAtUtc=null;user.UpdatedAtUtc=now;
  if(brand is not null){brand.Status="Rejected";brand.UpdatedAtUtc=now;}
  if(koc is not null){koc.Status="Rejected";koc.IsVisible=false;koc.UpdatedAtUtc=now;}
  var sessions=await db.AuthSessions.Where(x=>x.UserId==userId&&!x.RevokedAtUtc.HasValue).ToListAsync(ct);
  foreach(var session in sessions){session.RevokedAtUtc=now;session.RevokeReason=reason;session.UpdatedAtUtc=now;}
  db.AuditLogs.Add(new(){ActorUserId=admin,EntityName="User",EntityId=userId.ToString(),ActionName="SoftDelete",PayloadJson=JsonSerializer.Serialize(new{profileType,FromStatus=fromStatus,ToStatus="Disabled",FromProfileStatus=fromProfileStatus,ToProfileStatus="Rejected",Reason=reason,RevokedSessions=sessions.Count}),CreatedAtUtc=now});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
  return new(true,"Đã xóa tài khoản khỏi hệ thống vận hành và thu hồi toàn bộ phiên đăng nhập.");
 }
 private static UserAccountAdminResult Fail(string message)=>new(false,message);
 private static string CleanReason(string? value){var result=value?.Trim()??"";return result.Length>1000?result[..1000]:result;}
}
