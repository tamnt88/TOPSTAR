namespace Topstar.Infrastructure.Entities;

public sealed class PermissionEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class RolePermissionEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class UserPermissionEntity
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsAllowed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class MenuEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public int OrderNo { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class RoleMenuEntity
{
    public Guid RoleId { get; set; }
    public Guid MenuId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class UserMenuEntity
{
    public Guid UserId { get; set; }
    public Guid MenuId { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class DashboardWidgetEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DataSource { get; set; }
    public string? RoleScope { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class RoleDashboardConfigEntity
{
    public Guid RoleId { get; set; }
    public Guid WidgetId { get; set; }
    public int Position { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class UserDashboardConfigEntity
{
    public Guid UserId { get; set; }
    public Guid WidgetId { get; set; }
    public int Position { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
