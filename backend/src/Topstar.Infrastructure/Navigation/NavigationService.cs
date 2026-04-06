using Microsoft.EntityFrameworkCore;
using Topstar.Application.Navigation;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Navigation;

public sealed class NavigationService(TopstarDbContext db) : INavigationService
{
    public async Task<IReadOnlyList<NavigationMenuItem>> GetMenusByRoleAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(roleCode) ? "ADMIN" : roleCode.Trim().ToUpperInvariant();

        var roles = await db.Roles
            .AsNoTracking()
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(cancellationToken);

        var roleId = roles
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .FirstOrDefault(x => string.Equals(x.Code, normalizedRole, StringComparison.OrdinalIgnoreCase))
            ?.Id;

        if (!roleId.HasValue)
        {
            return Array.Empty<NavigationMenuItem>();
        }

        var menuIds = await db.RoleMenus
            .AsNoTracking()
            .Where(x => x.RoleId == roleId.Value)
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return Array.Empty<NavigationMenuItem>();
        }

        return await db.Menus
            .AsNoTracking()
            .Where(x => x.IsVisible && menuIds.Contains(x.Id))
            .OrderBy(x => x.OrderNo)
            .ThenBy(x => x.Title)
            .Select(x => new NavigationMenuItem(x.Id, x.Title, x.Path, x.Icon, x.ParentId, x.OrderNo))
            .ToListAsync(cancellationToken);
    }
}
