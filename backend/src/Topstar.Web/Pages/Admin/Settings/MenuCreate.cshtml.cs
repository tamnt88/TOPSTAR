using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Menus;
using Topstar.Core.Common.Pagination;
using Topstar.Core.Menus;
using Topstar.Web.Features.Admin.Menu;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class MenuCreateModel(IMenuAdminService menuAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet = true)] public string Scope { get; set; } = MenuScopeOptions.All;
    [BindProperty(SupportsGet = true)] public string Visible { get; set; } = "ALL";
    [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = PaginationDefaults.DefaultPageSize;

    [BindProperty] public MenuFormInput Input { get; set; } = new();
    public IReadOnlyList<MenuParentVm> ParentOptions { get; private set; } = [];
    public IReadOnlyList<MenuScopeItem> ScopeOptions { get; } = MenuScopeOptions.UiOptions.Where(x => x.Value != MenuScopeOptions.All).ToList();

    public async Task OnGetAsync() => await LoadParentsAsync();

    public async Task<IActionResult> OnGetParentOptionsAsync(string? scopeCode)
    {
        var items = BuildParentOptions(await menuAdminService.GetParentOptionsAsync(scopeCode, HttpContext.RequestAborted))
            .Select(x => new { id = x.Id, text = x.DisplayText })
            .ToList();

        return new JsonResult(new { data = items });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadParentsAsync();

        var rs = await menuAdminService.CreateAsync(new MenuUpsertRequest(
            null,
            Input.ScopeCode,
            Input.Title,
            Input.Path,
            Input.Icon,
            Input.ParentId,
            Input.OrderNo,
            Input.IsVisible), HttpContext.RequestAborted);

        if (!rs.Success)
        {
            TempData["AdminMessageError"] = rs.Message;
            return Page();
        }

        TempData["AdminMessageSuccess"] = rs.Message;
        return RedirectToPage("/Admin/Settings/MenuManagement", new { keyword = Keyword, scope = Scope, visible = Visible, pageIndex = PageIndex, pageSize = PageSize });
    }

    private async Task LoadParentsAsync()
    {
        ParentOptions = BuildParentOptions(await menuAdminService.GetParentOptionsAsync(Input.ScopeCode, HttpContext.RequestAborted));
    }

    private static IReadOnlyList<MenuParentVm> BuildParentOptions(IReadOnlyList<MenuParentOption> items)
    {
        var roots = items
            .Where(x => !x.ParentId.HasValue)
            .OrderBy(x => x.OrderNo)
            .ThenBy(x => x.Title)
            .ToList();

        var childrenMap = items
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.OrderNo).ThenBy(x => x.Title).ToList());

        var result = new List<MenuParentVm>();
        var visited = new HashSet<Guid>();

        void Traverse(Guid parentId, int depth)
        {
            if (!childrenMap.TryGetValue(parentId, out var list))
            {
                return;
            }

            foreach (var item in list)
            {
                if (!visited.Add(item.Id))
                {
                    continue;
                }

                var prefix = depth <= 0 ? string.Empty : new string('-', depth * 2) + " ";
                result.Add(new MenuParentVm(item.Id, prefix + item.Title));
                Traverse(item.Id, depth + 1);
            }
        }

        foreach (var root in roots)
        {
            if (!visited.Add(root.Id))
            {
                continue;
            }

            result.Add(new MenuParentVm(root.Id, root.Title));
            Traverse(root.Id, 1);
        }

        foreach (var item in items.Where(x => !visited.Contains(x.Id)).OrderBy(x => x.OrderNo).ThenBy(x => x.Title))
        {
            result.Add(new MenuParentVm(item.Id, item.Title));
        }

        return result;
    }
}
