using Topstar.Core.Common.Pagination;

namespace Topstar.Web.Features.Common.Paging;

public sealed class PagerViewModel
{
    public string PageName { get; set; } = string.Empty;
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = PaginationDefaults.DefaultPageSize;
    public IReadOnlyList<int> PageSizeOptions { get; set; } = PaginationDefaults.PageSizeOptions;
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>();
}

