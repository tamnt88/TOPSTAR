using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Menus;
using Topstar.Core.Common.Pagination;
using Topstar.Core.Menus;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class MenuManagementModel(IMenuAdminService menuAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet = true)] public string Scope { get; set; } = MenuScopeOptions.All;
    [BindProperty(SupportsGet = true)] public string Visible { get; set; } = "ALL";
    [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = PaginationDefaults.DefaultPageSize;
    [BindProperty] public Guid DeleteId { get; set; }

    public int TotalItems { get; private set; }
    public int TotalPages { get; private set; }
    public IReadOnlyList<MenuRow> Rows { get; private set; } = [];
    public IReadOnlyList<MenuScopeItem> ScopeOptions { get; } = MenuScopeOptions.UiOptions;
    public IReadOnlyList<int> PageSizeOptions { get; } = PaginationDefaults.PageSizeOptions;

    public sealed record MenuRow(Guid Id, string Code, string Title, string? Path, string ParentTitle, int OrderNo, bool IsVisible, string ScopeTag);

    public async Task OnGetAsync()
    {
        var rs = await menuAdminService.GetListAsync(new MenuListQuery(Keyword, Scope, Visible, PageIndex, PageSize), HttpContext.RequestAborted);
        TotalItems = rs.TotalItems;
        TotalPages = rs.TotalPages;
        PageIndex = rs.PageIndex;
        PageSize = rs.PageSize;
        Scope = MenuScopeOptions.Normalize(Scope, allowAll: true);
        Rows = rs.Items.Select(x => new MenuRow(x.Id, x.Code, x.Title, x.Path, x.ParentTitle, x.OrderNo, x.IsVisible, x.ScopeTag)).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var rs = await menuAdminService.DeleteAsync(DeleteId, HttpContext.RequestAborted);
        TempData[rs.Success ? "AdminMessageSuccess" : "AdminMessageError"] = rs.Message;
        return RedirectWithFilters();
    }

    private RedirectToPageResult RedirectWithFilters()
        => RedirectToPage("/Admin/Settings/MenuManagement", new { keyword = Keyword, scope = Scope, visible = Visible, pageIndex = PageIndex, pageSize = PageSize });
}
