using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Topstar.Application;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Auth;

internal sealed class AccountActivationService(TopstarDbContext dbContext) : IAccountActivationService
{
    public async Task<AuthResult<ActivationTokenResult>> CreateActivationTokenByEmailAsync(string email, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        email = email?.Trim().ToLowerInvariant() ?? string.Empty;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || user.EmailConfirmed)
            return AuthResult<ActivationTokenResult>.Fail("Không thể tạo mã kích hoạt.");
        var recentSince = DateTime.UtcNow.AddMinutes(-15);
        var recentCount = await dbContext.EmailVerificationTokens.CountAsync(
            x => x.UserId == user.Id && x.CreatedAtUtc >= recentSince, cancellationToken);
        if (!AccountActivationRules.CanCreateChallenge(recentCount))
            return AuthResult<ActivationTokenResult>.Fail("Đã vượt giới hạn gửi lại. Vui lòng thử sau 15 phút.");
        var result = await CreateActivationTokenAsync(user.Id, ttl, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<AuthResult<ActivationTokenResult>> CreateActivationTokenAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return AuthResult<ActivationTokenResult>.Fail("Không tìm thấy tài khoản.");
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var tokenHash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.Add(ttl);

        dbContext.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CodeHash = HashCode(userId, rawCode),
            AttemptCount = 0,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult<ActivationTokenResult>.Ok(new ActivationTokenResult(rawToken, rawCode, expiresAt));
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

    public async Task<AuthResult<bool>> ActivateByCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        email = email?.Trim().ToLowerInvariant() ?? string.Empty;
        code = code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !AccountActivationRules.IsValidOtp(code))
            return AuthResult<bool>.Fail("Email hoặc OTP không hợp lệ.");

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null) return AuthResult<bool>.Fail("Email hoặc OTP không hợp lệ.");
        if (user.EmailConfirmed) return AuthResult<bool>.Ok(true, "Tài khoản đã được kích hoạt trước đó.");

        var verification = await dbContext.EmailVerificationTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow && x.CodeHash != null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (verification is null) return AuthResult<bool>.Fail("OTP không tồn tại hoặc đã hết hạn.");
        if (verification.AttemptCount >= AccountActivationRules.MaxOtpAttempts) return AuthResult<bool>.Fail("OTP đã bị khóa do nhập sai quá nhiều lần.");

        var expected = Convert.FromHexString(verification.CodeHash!);
        var supplied = Convert.FromHexString(HashCode(user.Id, code));
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            verification.AttemptCount++;
            verification.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return AuthResult<bool>.Fail("OTP không đúng.");
        }

        var now = DateTime.UtcNow;
        user.EmailConfirmed = true;
        user.UpdatedAtUtc = now;
        verification.UsedAtUtc = now;
        verification.UpdatedAtUtc = now;
        await dbContext.EmailVerificationTokens.Where(x => x.UserId == user.Id && x.Id != verification.Id && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now).SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult<bool>.Ok(true, "Kích hoạt tài khoản thành công. Bạn có thể đăng nhập ngay.");
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashCode(Guid userId, string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));
}
