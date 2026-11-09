namespace Topstar.Infrastructure.Entities;
public sealed class UserAccountStatusHistoryEntity
{
 public Guid Id{get;set;}public Guid UserId{get;set;}public string FromStatus{get;set;}=string.Empty;public string ToStatus{get;set;}=string.Empty;
 public string ProfileType{get;set;}=string.Empty;public string FromProfileStatus{get;set;}=string.Empty;public string ToProfileStatus{get;set;}=string.Empty;
 public string Reason{get;set;}=string.Empty;public Guid ActorUserId{get;set;}public DateTime CreatedAtUtc{get;set;}
}
