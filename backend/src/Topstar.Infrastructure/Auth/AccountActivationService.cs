using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Topstar.Application;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Auth;

internal sealed class AccountActivationService(TopstarDbContext dbContext) : IAccountActivationService
{
    public async Task<AuthResult<ActivationTokenResult>> CreateActivationTokenAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return AuthResult<ActivationTokenResult>.Fail("Không tìm thấy tài khoản.");
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var tokenHash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.Add(ttl);

        dbContext.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult<ActivationTokenResult>.Ok(new ActivationTokenResult(rawToken, expiresAt));
    }

    public async Task<AuthResult<bool>> ActivateByTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return AuthResult<bool>.Fail("Thiếu mã kích hoạt.");
        }

        var tokenHash = HashToken(rawToken.Trim());
        var verification = await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (verification is null)
        {
            return AuthResult<bool>.Fail("Mã kích hoạt không tồn tại.");
        }

        if (verification.UsedAtUtc.HasValue)
        {
            return AuthResult<bool>.Fail("Mã kích hoạt đã được sử dụng.");
        }

        if (verification.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return AuthResult<bool>.Fail("Mã kích hoạt đã hết hạn.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == verification.UserId, cancellationToken);
        if (user is null)
        {
            return AuthResult<bool>.Fail("Không tìm thấy tài khoản.");
        }

        user.EmailConfirmed = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        verification.UsedAtUtc = DateTime.UtcNow;
        verification.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult<bool>.Ok(true, "Kích hoạt tài khoản thành công. Bạn có thể đăng nhập ngay.");
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
