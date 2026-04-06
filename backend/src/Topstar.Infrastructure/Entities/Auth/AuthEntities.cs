namespace Topstar.Infrastructure.Entities;

public sealed class UserDeviceEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public bool IsTrusted { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AuthEventEntity
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? EmailOrPhone { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public Guid? UserDeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AuthSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? UserDeviceId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PasswordResetTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public Guid? RequestedByDeviceId { get; set; }
    public string? RequestedIpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class EmailVerificationTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
