using System.Security.Cryptography;
using System.Text;

namespace Topstar.Application;

internal sealed class AuthService(IAuthRepository authRepository) : IAuthService
{
    public async Task<AuthResult<AuthenticatedUser>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var lookup = request.EmailOrPhone.Trim();
        var user = await authRepository.FindUserByEmailOrPhoneAsync(lookup, cancellationToken);

        if (user is null)
        {
            await authRepository.AddAuthEventAsync(null, lookup, "Login", "Failed", "Tài khoản không tồn tại", null, request.Device, cancellationToken);
            return AuthResult<AuthenticatedUser>.Fail("Tài khoản không tồn tại.");
        }

        if (!user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            var deviceId = await authRepository.UpsertDeviceAsync(user.UserId, request.Device, cancellationToken);
            await authRepository.AddAuthEventAsync(user.UserId, lookup, "Login", "Blocked", "Tài khoản đang bị khóa hoặc vô hiệu hóa", deviceId, request.Device, cancellationToken);
            return AuthResult<AuthenticatedUser>.Fail("Tài khoản đang bị khóa hoặc vô hiệu hóa.");
        }

        if (user.LockoutEndAtUtc.HasValue && user.LockoutEndAtUtc.Value > DateTime.UtcNow)
        {
            var deviceId = await authRepository.UpsertDeviceAsync(user.UserId, request.Device, cancellationToken);
            await authRepository.AddAuthEventAsync(user.UserId, lookup, "Login", "Blocked", "Tài khoản tạm khóa do đăng nhập sai nhiều lần", deviceId, request.Device, cancellationToken);
            return AuthResult<AuthenticatedUser>.Fail("Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.");
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await authRepository.UpdateLoginFailedAsync(user.UserId, cancellationToken);
            var deviceId = await authRepository.UpsertDeviceAsync(user.UserId, request.Device, cancellationToken);
            await authRepository.AddAuthEventAsync(user.UserId, lookup, "Login", "Failed", "Sai mật khẩu", deviceId, request.Device, cancellationToken);
            return AuthResult<AuthenticatedUser>.Fail("Mật khẩu không chính xác.");
        }

        await authRepository.UpdateLoginSuccessAsync(user.UserId, cancellationToken);
        var successDeviceId = await authRepository.UpsertDeviceAsync(user.UserId, request.Device, cancellationToken);
        await authRepository.AddAuthEventAsync(user.UserId, lookup, "Login", "Success", null, successDeviceId, request.Device, cancellationToken);

        return AuthResult<AuthenticatedUser>.Ok(MapToAuthenticated(user), "Đăng nhập thành công.");
    }

    public async Task<AuthResult<AuthenticatedUser>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone?.Trim();

        if (await authRepository.EmailExistsAsync(email, cancellationToken))
        {
            return AuthResult<AuthenticatedUser>.Fail("Email đã được sử dụng.");
        }

        if (!string.IsNullOrWhiteSpace(phone) && await authRepository.PhoneExistsAsync(phone, cancellationToken))
        {
            return AuthResult<AuthenticatedUser>.Fail("Số điện thoại đã được sử dụng.");
        }

        var hash = PasswordHasher.HashPassword(request.Password);
        var created = await authRepository.CreateUserAsync(request with { Email = email, Phone = phone }, hash, cancellationToken);

        var deviceId = await authRepository.UpsertDeviceAsync(created.UserId, request.Device, cancellationToken);
        await authRepository.AddAuthEventAsync(created.UserId, email, "Register", "Success", null, deviceId, request.Device, cancellationToken);

        return AuthResult<AuthenticatedUser>.Ok(MapToAuthenticated(created), "Đăng ký thành công.");
    }

    public async Task<AuthResult<ForgotPasswordResult>> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var lookup = request.EmailOrPhone.Trim();
        var user = await authRepository.FindUserByEmailOrPhoneAsync(lookup, cancellationToken);

        if (user is null)
        {
            await authRepository.AddAuthEventAsync(null, lookup, "ForgotPassword", "Failed", "Tài khoản không tồn tại", null, request.Device, cancellationToken);
            return AuthResult<ForgotPasswordResult>.Fail("Không tìm thấy tài khoản.");
        }

        var deviceId = await authRepository.UpsertDeviceAsync(user.UserId, request.Device, cancellationToken);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var tokenHash = HashToken(token);
        var expires = DateTime.UtcNow.AddMinutes(30);

        await authRepository.CreatePasswordResetTokenAsync(user.UserId, tokenHash, expires, deviceId, request.Device.IpAddress, cancellationToken);
        await authRepository.AddAuthEventAsync(user.UserId, lookup, "ForgotPassword", "Success", null, deviceId, request.Device, cancellationToken);

        return AuthResult<ForgotPasswordResult>.Ok(new ForgotPasswordResult(token, expires, user.Email), "Yêu cầu đặt lại mật khẩu thành công.");
    }

    public async Task<AuthResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.Token.Trim());
        var tokenRow = await authRepository.FindValidPasswordResetTokenAsync(tokenHash, cancellationToken);

        if (tokenRow is null || tokenRow.UsedAtUtc.HasValue || tokenRow.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await authRepository.AddAuthEventAsync(null, null, "ResetPassword", "Failed", "Token không hợp lệ hoặc đã hết hạn", null, request.Device, cancellationToken);
            return AuthResult<bool>.Fail("Token không hợp lệ hoặc đã hết hạn.");
        }

        var hash = PasswordHasher.HashPassword(request.NewPassword);
        await authRepository.UpdatePasswordAsync(tokenRow.UserId, hash, cancellationToken);
        await authRepository.MarkPasswordResetTokenUsedAsync(tokenRow.TokenId, cancellationToken);

        var deviceId = await authRepository.UpsertDeviceAsync(tokenRow.UserId, request.Device, cancellationToken);
        await authRepository.AddAuthEventAsync(tokenRow.UserId, null, "ResetPassword", "Success", null, deviceId, request.Device, cancellationToken);

        return AuthResult<bool>.Ok(true, "Đặt lại mật khẩu thành công.");
    }

    private static AuthenticatedUser MapToAuthenticated(AuthUserRow row)
    {
        var role = row.RoleCode.ToUpperInvariant() switch
        {
            "ADMIN" => UserRoleType.Admin,
            "BRAND" => UserRoleType.Brand,
            _ => UserRoleType.Koc
        };

        return new AuthenticatedUser(row.UserId, row.Email, row.Phone, role, row.DisplayName);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}


