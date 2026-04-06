namespace Topstar.Application;

public enum UserRoleType
{
    Koc,
    Brand,
    Admin
}

public sealed record DeviceContext(
    string DeviceFingerprint,
    string? DeviceName,
    string? DeviceType,
    string? OsName,
    string? OsVersion,
    string? BrowserName,
    string? BrowserVersion,
    string? UserAgent,
    string? IpAddress,
    string? CountryCode,
    string? City
);

public sealed record LoginRequest(string EmailOrPhone, string Password, DeviceContext Device);

public sealed record RegisterRequest(
    UserRoleType Role,
    string Email,
    string? Phone,
    string? Gender,
    string Password,
    string FullName,
    string? CompanyName,
    string? ContactPhone,
    DeviceContext Device
);

public sealed record ForgotPasswordRequest(string EmailOrPhone, DeviceContext Device);

public sealed record ResetPasswordRequest(string Token, string NewPassword, DeviceContext Device);

public sealed record AuthenticatedUser(Guid UserId, string Email, string? Phone, UserRoleType Role, string DisplayName);

public sealed record AuthResult<T>(bool Success, string Message, T? Data)
{
    public static AuthResult<T> Ok(T data, string message = "OK") => new(true, message, data);
    public static AuthResult<T> Fail(string message) => new(false, message, default);
}

public sealed record ForgotPasswordResult(string ResetToken, DateTime ExpiresAtUtc, string UserEmail);

public interface IAuthService
{
    Task<AuthResult<AuthenticatedUser>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthenticatedUser>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<ForgotPasswordResult>> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IAuthRepository
{
    Task<AuthUserRow?> FindUserByEmailOrPhoneAsync(string emailOrPhone, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken);
    Task<AuthUserRow> CreateUserAsync(RegisterRequest request, string passwordHash, CancellationToken cancellationToken);
    Task UpdateLoginSuccessAsync(Guid userId, CancellationToken cancellationToken);
    Task UpdateLoginFailedAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid> UpsertDeviceAsync(Guid? userId, DeviceContext device, CancellationToken cancellationToken);
    Task AddAuthEventAsync(Guid? userId, string? emailOrPhone, string eventType, string eventStatus, string? failureReason, Guid? userDeviceId, DeviceContext device, CancellationToken cancellationToken);
    Task CreatePasswordResetTokenAsync(Guid userId, string tokenHash, DateTime expiresAtUtc, Guid? requestedByDeviceId, string? requestedIpAddress, CancellationToken cancellationToken);
    Task<PasswordResetTokenRow?> FindValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task MarkPasswordResetTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken);
    Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken);
}

public sealed record AuthUserRow(
    Guid UserId,
    string Email,
    string? Phone,
    string PasswordHash,
    string Status,
    DateTime? LockoutEndAtUtc,
    string RoleCode,
    string DisplayName
);

public sealed record PasswordResetTokenRow(Guid TokenId, Guid UserId, DateTime ExpiresAtUtc, DateTime? UsedAtUtc);


