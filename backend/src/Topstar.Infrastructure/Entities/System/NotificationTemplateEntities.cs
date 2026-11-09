namespace Topstar.Infrastructure.Entities;
public sealed class NotificationTemplateEntity
{
 public Guid Id{get;set;}public string Type{get;set;}=string.Empty;public string AudienceRole{get;set;}=string.Empty;public string TitleTemplate{get;set;}=string.Empty;public string BodyTemplate{get;set;}=string.Empty;public bool IsActive{get;set;}public Guid CreatedByUserId{get;set;}public Guid? UpdatedByUserId{get;set;}public DateTime CreatedAtUtc{get;set;}public DateTime? UpdatedAtUtc{get;set;}
}
public sealed class NotificationTemplateHistoryEntity
{
 public Guid Id{get;set;}public Guid TemplateId{get;set;}public string TitleTemplate{get;set;}=string.Empty;public string BodyTemplate{get;set;}=string.Empty;public bool IsActive{get;set;}public string Reason{get;set;}=string.Empty;public Guid ActorUserId{get;set;}public DateTime CreatedAtUtc{get;set;}
}
