using Microsoft.EntityFrameworkCore;
using Topstar.Application.Menus;
using Topstar.Core.Common.Pagination;
using Topstar.Core.Menus;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Menus;

public sealed class MenuAdminService(TopstarDbContext db, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) : IMenuAdminService
{
    private void InvalidateNavigation()
    {
        foreach (var role in new[] { "ADMIN", "KOC", "BRAND" })
            cache.Remove(Topstar.Infrastructure.Navigation.NavigationService.CacheKey(role));
    }
    public async Task<MenuListResult> GetListAsync(MenuListQuery query, CancellationToken cancellationToken = default)
    {
        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = query.PageSize is < PaginationDefaults.MinPageSize or > PaginationDefaults.MaxPageSize
            ? PaginationDefaults.DefaultPageSize
            : query.PageSize;
        var scope = MenuScopeOptions.Normalize(query.Scope, allowAll: true);
        var visible = string.IsNullOrWhiteSpace(query.Visible) ? "ALL" : query.Visible.ToUpperInvariant();

        var q = db.Menus.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(x => x.Code.Contains(k) || x.Title.Contains(k) || (x.Path != null && x.Path.Contains(k)));
        }

        if (!visible.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var show = visible.Equals("YES", StringComparison.OrdinalIgnoreCase);
            q = q.Where(x => x.IsVisible == show);
        }

        if (!scope.Equals(MenuScopeOptions.All, StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.Code.StartsWith($"MENU_{scope}_"));
        }

        var totalItems = await q.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        if (pageIndex > totalPages) pageIndex = totalPages;

        var items = await q.OrderBy(x => x.OrderNo).ThenBy(x => x.Title)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.Code, x.Title, x.Path, x.ParentId, x.OrderNo, x.IsVisible })
            .ToListAsync(cancellationToken);

        var parentIds = items.Where(x => x.ParentId.HasValue).Select(x => x.ParentId!.Value).Distinct().ToList();
        var parentMap = await db.Menus.AsNoTracking().Where(x => parentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var mapped = items.Select(x =>
        {
            var scopeTag = MenuCodeFormatter.ResolveScopeTag(x.Code);
            var parentTitle = x.ParentId.HasValue && parentMap.TryGetValue(x.ParentId.Value, out var t) ? t : "-";
            return new MenuListItem(x.Id, x.Code, x.Title, x.Path, parentTitle, x.OrderNo, x.IsVisible, scopeTag);
        }).ToList();

        return new MenuListResult(mapped, totalItems, totalPages, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<MenuParentOption>> GetParentOptionsAsync(string? scopeCode, CancellationToken cancellationToken = default)
    {
        var normalizedScope = MenuScopeOptions.Normalize(scopeCode, allowAll: false);
        var query = db.Menus.AsNoTracking().Where(x => x.Code.StartsWith($"MENU_{normalizedScope}_"));
        return await query
            .OrderBy(x => x.OrderNo)
            .ThenBy(x => x.Title)
            .Select(x => new MenuParentOption(
                x.Id,
                x.Title,
                x.ParentId,
                x.OrderNo))
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuEditData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.Menus.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new MenuEditData(x.Id, x.Code, x.Title, x.Path, x.Icon, x.ParentId, x.OrderNo, x.IsVisible))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(MenuUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return new ServiceResult(false, "Vui lòng nhập đầy đủ Phạm vi và Tiêu đề.");

        var code = await GenerateUniqueCodeAsync(request.ScopeCode, request.Title, null, cancellationToken);

        var now = DateTime.UtcNow;
        db.Menus.Add(new MenuEntity
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = request.Title.Trim(),
            Path = string.IsNullOrWhiteSpace(request.Path) ? null : request.Path.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            ParentId = request.ParentId,
            OrderNo = request.OrderNo,
            IsVisible = request.IsVisible,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
        InvalidateNavigation();
        return new ServiceResult(true, "Đã thêm menu thành công.");
    }

    public async Task<ServiceResult> UpdateAsync(MenuUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Id.HasValue || request.Id.Value == Guid.Empty)
            return new ServiceResult(false, "Không tìm thấy menu cần cập nhật.");

        var entity = await db.Menus.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Menu không tồn tại.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return new ServiceResult(false, "Vui lòng nhập đầy đủ Phạm vi và Tiêu đề.");

        var code = await GenerateUniqueCodeAsync(request.ScopeCode, request.Title, request.Id.Value, cancellationToken);

        entity.Code = code;
        entity.Title = request.Title.Trim();
        entity.Path = string.IsNullOrWhiteSpace(request.Path) ? null : request.Path.Trim();
        entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        entity.ParentId = request.ParentId;
        entity.OrderNo = request.OrderNo;
        entity.IsVisible = request.IsVisible;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        InvalidateNavigation();
        return new ServiceResult(true, "Đã cập nhật menu thành công.");
    }

    private async Task<string> GenerateUniqueCodeAsync(string? scopeCode, string title, Guid? excludeId, CancellationToken cancellationToken)
    {
        var scope = MenuScopeOptions.Normalize(scopeCode, allowAll: false);
        var normalized = NormalizeToken(title);
        var baseCode = $"MENU_{scope}_{normalized}";
        var code = baseCode;
        var seed = 2;
        while (await db.Menus.AnyAsync(x => x.Code == code && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken))
        {
            code = $"{baseCode}_{seed}";
            seed++;
        }

        return code;
    }

    private static string NormalizeToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return $"MENU_{Guid.NewGuid():N}"[..12];
        }

        var chars = input.Trim().ToUpperInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();

        var raw = new string(chars);
        while (raw.Contains("__", StringComparison.Ordinal))
        {
            raw = raw.Replace("__", "_", StringComparison.Ordinal);
        }

        raw = raw.Trim('_');
        return string.IsNullOrWhiteSpace(raw) ? $"MENU_{Guid.NewGuid():N}"[..12] : raw;
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return new ServiceResult(false, "Không tìm thấy menu cần xóa.");

        if (await db.Menus.AnyAsync(x => x.ParentId == id, cancellationToken))
            return new ServiceResult(false, "Không thể xóa menu cha đang có menu con.");

        if (await db.RoleMenus.AnyAsync(x => x.MenuId == id, cancellationToken))
            return new ServiceResult(false, "Không thể xóa menu đang được gán cho vai trò.");

        var entity = await db.Menus.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Menu không tồn tại.");

        db.Menus.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        InvalidateNavigation();
        return new ServiceResult(true, "Đã xóa menu thành công.");
    }
}
