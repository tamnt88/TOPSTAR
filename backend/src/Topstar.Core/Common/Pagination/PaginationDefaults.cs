namespace Topstar.Core.Common.Pagination;

public static class PaginationDefaults
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 10;
    public const int MaxPageSize = 100;

    public static int Normalize(int value) => value is 10 or 20 or 50 or 100 ? value : DefaultPageSize;

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 20, 50, 100];
}
