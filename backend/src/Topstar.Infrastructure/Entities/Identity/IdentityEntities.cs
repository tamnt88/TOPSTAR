namespace Topstar.Infrastructure.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
    public KocProfileEntity? KocProfile { get; set; }
    public BrandProfileEntity? BrandProfile { get; set; }
    public AdminProfileEntity? AdminProfile { get; set; }
}

public sealed class RoleEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public UserEntity User { get; set; } = null!;
    public RoleEntity Role { get; set; } = null!;
}

public sealed class AdminProfileEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public UserEntity User { get; set; } = null!;
}
