namespace Topstar.Core.Common.Pagination;

public static class PaginationDefaults
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 20;
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyList<int> PageSizeOptions = [20, 50, 100];
}
