namespace Topstar.Application.Navigation;

public interface INavigationService
{
    Task<IReadOnlyList<NavigationMenuItem>> GetMenusByRoleAsync(string roleCode, CancellationToken cancellationToken = default);
}

public sealed record NavigationMenuItem(
    Guid Id,
    string Title,
    string? Path,
    string? Icon,
    Guid? ParentId,
    int OrderNo);
