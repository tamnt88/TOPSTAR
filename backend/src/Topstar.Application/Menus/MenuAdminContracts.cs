namespace Topstar.Application.Menus;

public interface IMenuAdminService
{
    Task<MenuListResult> GetListAsync(MenuListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuParentOption>> GetParentOptionsAsync(string? scopeCode, CancellationToken cancellationToken = default);
    Task<MenuEditData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(MenuUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(MenuUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record MenuListQuery(string? Keyword, string Scope, string Visible, int PageIndex, int PageSize);

public sealed record MenuListItem(
    Guid Id,
    string Code,
    string Title,
    string? Path,
    string ParentTitle,
    int OrderNo,
    bool IsVisible,
    string ScopeTag);

public sealed record MenuListResult(IReadOnlyList<MenuListItem> Items, int TotalItems, int TotalPages, int PageIndex, int PageSize);

public sealed record MenuParentOption(Guid Id, string Title, Guid? ParentId, int OrderNo);

public sealed record MenuEditData(
    Guid Id,
    string Code,
    string Title,
    string? Path,
    string? Icon,
    Guid? ParentId,
    int OrderNo,
    bool IsVisible);

public sealed record MenuUpsertRequest(
    Guid? Id,
    string ScopeCode,
    string Title,
    string? Path,
    string? Icon,
    Guid? ParentId,
    int OrderNo,
    bool IsVisible);

public sealed record ServiceResult(bool Success, string Message);
