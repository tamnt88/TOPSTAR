using Topstar.Infrastructure.Entities;

namespace Topstar.Infrastructure.Notifications;

internal static class NotificationFactory
{
    public static NotificationEntity Create(
        Guid userId,
        string type,
        string title,
        string body,
        DateTime createdAtUtc,
        string sourceEntityType,
        Guid sourceEntityId,
        string? targetUrl = null,
        string? audienceRole = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            IsVisible = true,
            AudienceRole = string.IsNullOrWhiteSpace(audienceRole) ? null : audienceRole.Trim().ToUpperInvariant(),
            TargetUrl = targetUrl,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            CreatedAtUtc = createdAtUtc
        };
}
