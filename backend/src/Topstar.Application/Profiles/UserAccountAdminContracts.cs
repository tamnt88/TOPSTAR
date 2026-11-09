namespace Topstar.Application.Profiles;
public static class UserAccountAdminRules
{
 public static bool CanChange(bool locked,string userStatus,string profileStatus)=>locked
  ?userStatus=="Active"&&profileStatus==ProfileStatuses.Approved
  :userStatus=="Locked";
}
public sealed record UserAccountAdminResult(bool Success,string Message);
public sealed record UserAccountAdminDetail(
 Guid UserId,string ProfileType,string DisplayName,string Email,string? Phone,string UserStatus,string ProfileStatus,
 bool EmailConfirmed,bool PhoneConfirmed,DateTime CreatedAtUtc,DateTime? LastLoginAtUtc,
 IReadOnlyList<UserSessionAdminItem> Sessions,IReadOnlyList<UserAuthEventAdminItem> AuthEvents,
 IReadOnlyList<UserAuditAdminItem> AuditLogs,IReadOnlyList<UserAdminNoteItem> Notes);
public sealed record UserSessionAdminItem(Guid Id,string Device,string? IpAddress,string? UserAgent,DateTime IssuedAtUtc,DateTime ExpiresAtUtc,DateTime? RevokedAtUtc,string? RevokeReason);
public sealed record UserAuthEventAdminItem(long Id,string EventType,string EventStatus,string? FailureReason,string? IpAddress,string? UserAgent,DateTime OccurredAtUtc);
public sealed record UserAuditAdminItem(long Id,string ActionName,string? ActorEmail,string? ActorRole,string? PayloadJson,DateTime CreatedAtUtc);
public sealed record UserAdminNoteItem(Guid Id,string Body,Guid ActorUserId,string? ActorEmail,DateTime CreatedAtUtc);
public interface IUserAccountAdminService
{
 Task<UserAccountAdminResult> ChangeLockAsync(Guid adminUserId,Guid userId,bool locked,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> UpdateDirectoryAsync(Guid adminUserId,Guid userId,string profileType,string name,string email,string? location,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> RecordCreateAsync(Guid adminUserId,Guid userId,string profileType,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminDetail?> GetDetailAsync(Guid userId,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> SetVerificationAsync(Guid adminUserId,Guid userId,bool? emailConfirmed,bool? phoneConfirmed,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> RevokeSessionAsync(Guid adminUserId,Guid userId,Guid? sessionId,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> AddNoteAsync(Guid adminUserId,Guid userId,string body,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> RecordSecurityActionAsync(Guid adminUserId,Guid userId,string action,string reason,CancellationToken cancellationToken=default);
 Task<UserAccountAdminResult> RemoveAsync(Guid adminUserId,Guid userId,string reason,CancellationToken cancellationToken=default);
}
