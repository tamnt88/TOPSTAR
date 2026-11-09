namespace Topstar.Application.Notifications;
public sealed record NotificationEventItem(string EventType,DateTime CreatedAtUtc,string? TargetUrlSnapshot,string? ActorRoleSnapshot);
public sealed record NotificationItem(Guid Id,string Type,string Title,string Body,bool IsRead,bool IsVisible,DateTime? ReadAtUtc,DateTime? HiddenAtUtc,DateTime CreatedAtUtc,string? TargetUrl,string? SourceEntityType,Guid? SourceEntityId,IReadOnlyList<NotificationEventItem> History);
public sealed record NotificationInbox(int UnreadCount,IReadOnlyList<NotificationItem> Items);
public sealed record NotificationPage(int TotalCount, int FilteredCount, int UnreadCount, int ActiveCount, int ArchivedCount, IReadOnlyList<NotificationItem> Items);
public sealed record NotificationResult(bool Success,string Message);
public sealed record NotificationOpenResult(bool Success,string Message,string? TargetUrl);
public interface INotificationInboxService
{
 Task<int> GetUnreadCountAsync(Guid userId,string role,CancellationToken ct=default);
 Task<NotificationInbox> GetInboxAsync(Guid userId,string role,string? filter,CancellationToken ct=default);
 Task<NotificationPage> GetPageAsync(Guid userId, string role, string? filter, string? search, int start, int length, CancellationToken ct=default, string? category=null);
 Task<NotificationResult> MarkReadAsync(Guid userId,string role,Guid notificationId,CancellationToken ct=default);
 Task<NotificationResult> MarkAllReadAsync(Guid userId,string role,CancellationToken ct=default);
 Task<NotificationResult> ArchiveAsync(Guid userId,string role,Guid notificationId,CancellationToken ct=default);
 Task<NotificationResult> RestoreAsync(Guid userId,string role,Guid notificationId,CancellationToken ct=default);
 Task<NotificationOpenResult> OpenAsync(Guid userId,string role,Guid notificationId,CancellationToken ct=default);
}
