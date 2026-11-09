namespace Topstar.Application.Notifications;
public static class NotificationTemplateRules
{
 public static readonly string[] Roles=["ADMIN","BRAND","KOC"];
 public static bool IsValidRole(string? role)=>role is not null&&Roles.Contains(role,StringComparer.Ordinal);
 public static string Render(string template,string title,string body,int maxLength)
 {
  var value=template.Replace("{Title}",title,StringComparison.Ordinal).Replace("{Body}",body,StringComparison.Ordinal).Trim();
  return value.Length<=maxLength?value:value[..maxLength];
 }
 public static bool HasOnlySupportedPlaceholders(string value)
 {
  var cleaned=value.Replace("{Title}","",StringComparison.Ordinal).Replace("{Body}","",StringComparison.Ordinal);
  return !cleaned.Contains('{')&&!cleaned.Contains('}');
 }
}
public sealed record NotificationTemplateHistoryItem(string TitleTemplate,string BodyTemplate,bool IsActive,string Reason,string Actor,DateTime CreatedAtUtc);
public sealed record NotificationTemplateItem(Guid Id,string Type,string AudienceRole,string TitleTemplate,string BodyTemplate,bool IsActive,IReadOnlyList<NotificationTemplateHistoryItem> History);
public sealed record NotificationTemplateResult(bool Success,string Message);
public interface INotificationTemplateAdminService
{
 Task<IReadOnlyList<NotificationTemplateItem>> GetAllAsync(CancellationToken cancellationToken=default);
 Task<NotificationTemplateResult> SaveAsync(Guid actorUserId,Guid? id,string type,string audienceRole,string titleTemplate,string bodyTemplate,bool isActive,string reason,CancellationToken cancellationToken=default);
}
