using Microsoft.EntityFrameworkCore;
using Topstar.Application;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure;

internal sealed class EfAuthRepository(TopstarDbContext dbContext) : IAuthRepository
{
    public async Task<AuthUserRow?> FindUserByEmailOrPhoneAsync(string emailOrPhone, CancellationToken cancellationToken)
    {
        var normalized = emailOrPhone.Trim().ToLower();

        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.KocProfile)
            .Include(x => x.BrandProfile)
            .Include(x => x.AdminProfile)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalized || x.Phone == emailOrPhone, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = user.UserRoles
            .Select(x => x.Role.Code)
            .OrderBy(x => x == "ADMIN" ? 1 : x == "BRAND" ? 2 : 3)
            .FirstOrDefault() ?? "KOC";

        var displayName = user.KocProfile?.DisplayName
            ?? user.BrandProfile?.ContactName
            ?? user.AdminProfile?.FullName
            ?? user.Email;

        return new AuthUserRow(user.Id, user.Email, user.Phone, user.PasswordHash, user.Status, user.EmailConfirmed, user.LockoutEndAtUtc, role, displayName);
    }

    public async Task<AuthUserRow?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Include(x => x.KocProfile)
            .Include(x => x.BrandProfile)
            .Include(x => x.AdminProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null) return null;

        var role = user.UserRoles.Select(x => x.Role.Code)
            .OrderBy(x => x == "ADMIN" ? 1 : x == "BRAND" ? 2 : 3)
            .FirstOrDefault() ?? "KOC";
        var displayName = user.KocProfile?.DisplayName
            ?? user.BrandProfile?.ContactName
            ?? user.AdminProfile?.FullName
            ?? user.Email;

        return new AuthUserRow(user.Id, user.Email, user.Phone, user.PasswordHash, user.Status, user.EmailConfirmed, user.LockoutEndAtUtc, role, displayName);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(x => x.Phone == phone, cancellationToken);

    public async Task<AuthUserRow> CreateUserAsync(RegisterRequest request, string passwordHash, CancellationToken cancellationToken)
    {
        var roleCode = request.Role switch
        {
            UserRoleType.Admin => "ADMIN",
            UserRoleType.Brand => "BRAND",
            _ => "KOC"
        };

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == roleCode, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy role '{roleCode}'. Hãy chạy seed SQL.");

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Phone = request.Phone,
            Gender = request.Gender,
            PasswordHash = passwordHash,
            Status = "Active",
            EmailConfirmed = false,
            PhoneConfirmed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new UserRoleEntity
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });

        var displayName = request.FullName;
        if (request.Role == UserRoleType.Brand)
        {
            dbContext.BrandProfiles.Add(new BrandProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = request.CompanyName ?? request.FullName,
                ContactName = request.FullName,
                ContactPhone = request.ContactPhone ?? request.Phone ?? string.Empty,
                StreetAddress = request.StreetAddress,
                ProvinceId = request.ProvinceId,
                WardId = request.WardId,
                Status = "Draft",
                CreatedAtUtc = DateTime.UtcNow
            });

            displayName = request.CompanyName ?? request.FullName;
        }
        else if (request.Role == UserRoleType.Koc)
        {
            dbContext.KocProfiles.Add(new KocProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = request.FullName,
                Gender = request.Gender,
                StreetAddress = request.StreetAddress,
                ProvinceId = request.ProvinceId,
                WardId = request.WardId,
                Status = "Draft",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new AuthUserRow(user.Id, user.Email, user.Phone, user.PasswordHash, user.Status, user.EmailConfirmed, null, roleCode, displayName);
    }

    public async Task UpdateLoginSuccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.LastLoginAtUtc = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.LockoutEndAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        if (user.EmailConfirmed) return;
        user.EmailConfirmed = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLoginFailedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.FailedLoginCount += 1;
        if (user.FailedLoginCount >= 5)
        {
            user.LockoutEndAtUtc = DateTime.UtcNow.AddMinutes(15);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> UpsertDeviceAsync(Guid? userId, DeviceContext device, CancellationToken cancellationToken)
    {
        var fingerprint = string.IsNullOrWhiteSpace(device.DeviceFingerprint)
            ? $"fp-{Guid.NewGuid():N}"
            : device.DeviceFingerprint;

        var entity = await dbContext.UserDevices.FirstOrDefaultAsync(x => x.DeviceFingerprint == fingerprint, cancellationToken);
        if (entity is null)
        {
            entity = new UserDeviceEntity
            {
                Id = Guid.NewGuid(),
                DeviceFingerprint = fingerprint,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.UserDevices.Add(entity);
        }

        entity.UserId = userId ?? entity.UserId;
        entity.DeviceName = device.DeviceName;
        entity.DeviceType = device.DeviceType;
        entity.OsName = device.OsName;
        entity.OsVersion = device.OsVersion;
        entity.BrowserName = device.BrowserName;
        entity.BrowserVersion = device.BrowserVersion;
        entity.UserAgent = device.UserAgent;
        entity.IpAddress = device.IpAddress;
        entity.CountryCode = device.CountryCode;
        entity.City = device.City;
        entity.LastSeenAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task AddAuthEventAsync(Guid? userId, string? emailOrPhone, string eventType, string eventStatus, string? failureReason, Guid? userDeviceId, DeviceContext device, CancellationToken cancellationToken)
    {
        dbContext.AuthEvents.Add(new AuthEventEntity
        {
            UserId = userId,
            EmailOrPhone = emailOrPhone,
            EventType = eventType,
            EventStatus = eventStatus,
            FailureReason = failureReason,
            UserDeviceId = userDeviceId,
            IpAddress = device.IpAddress,
            UserAgent = device.UserAgent,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreatePasswordResetTokenAsync(Guid userId, string tokenHash, DateTime expiresAtUtc, Guid? requestedByDeviceId, string? requestedIpAddress, CancellationToken cancellationToken)
    {
        dbContext.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            RequestedByDeviceId = requestedByDeviceId,
            RequestedIpAddress = requestedIpAddress,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetTokenRow?> FindValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return token is null
            ? null
            : new PasswordResetTokenRow(token.Id, token.UserId, token.ExpiresAtUtc, token.UsedAtUtc);
    }

    public async Task MarkPasswordResetTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var token = await dbContext.PasswordResetTokens.FirstAsync(x => x.Id == tokenId, cancellationToken);
        token.UsedAtUtc = DateTime.UtcNow;
        token.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.PasswordHash = passwordHash;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.LockoutEndAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}



