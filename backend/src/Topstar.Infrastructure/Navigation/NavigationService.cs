using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Topstar.Application.Navigation;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Navigation;

public sealed class NavigationService(TopstarDbContext db, IMemoryCache cache) : INavigationService
{
    internal static string CacheKey(string roleCode) => $"navigation:role:{roleCode}";

    public async Task<IReadOnlyList<NavigationMenuItem>> GetMenusByRoleAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(roleCode) ? "ADMIN" : roleCode.Trim().ToUpperInvariant();

        if (cache.TryGetValue(CacheKey(normalizedRole), out IReadOnlyList<NavigationMenuItem>? cached) && cached is not null)
            return cached;

        var items = await (from role in db.Roles.AsNoTracking()
            join roleMenu in db.RoleMenus.AsNoTracking() on role.Id equals roleMenu.RoleId
            join menu in db.Menus.AsNoTracking() on roleMenu.MenuId equals menu.Id
            where role.Code == normalizedRole && menu.IsVisible
            select menu)
            .OrderBy(x => x.OrderNo)
            .ThenBy(x => x.Title)
            .Select(x => new NavigationMenuItem(x.Id, x.Title, x.Path, x.Icon, x.ParentId, x.OrderNo))
            .ToListAsync(cancellationToken);
        cache.Set(CacheKey(normalizedRole), (IReadOnlyList<NavigationMenuItem>)items, TimeSpan.FromMinutes(5));
        return items;
    }
}
