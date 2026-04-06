namespace Topstar.Application;

public sealed record ActivationTokenResult(string RawToken, DateTime ExpiresAtUtc);

public interface IAccountActivationService
{
    Task<AuthResult<ActivationTokenResult>> CreateActivationTokenAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ActivateByTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}
