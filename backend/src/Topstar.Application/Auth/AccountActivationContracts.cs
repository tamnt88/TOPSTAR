namespace Topstar.Application;

public static class AccountActivationRules
{
    public const int OtpLength = 6;
    public const int MaxOtpAttempts = 5;
    public const int MaxChallengesPerWindow = 3;
    public static bool IsValidOtp(string? code) => code?.Length == OtpLength && code.All(x => x is >= '0' and <= '9');
    public static bool CanCreateChallenge(int recentCount) => recentCount >= 0 && recentCount < MaxChallengesPerWindow;
}

public sealed record ActivationTokenResult(string RawToken, string RawCode, DateTime ExpiresAtUtc);

public interface IAccountActivationService
{
    Task<AuthResult<ActivationTokenResult>> CreateActivationTokenAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<AuthResult<ActivationTokenResult>> CreateActivationTokenByEmailAsync(string email, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ActivateByTokenAsync(string rawToken, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ActivateByCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}
