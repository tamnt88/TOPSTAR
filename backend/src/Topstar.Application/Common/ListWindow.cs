using Topstar.Core.Common.Pagination;

namespace Topstar.Application.Common;

/// <summary>Explicit per-list paging context. Counts are populated by the query service.</summary>
public sealed class ListWindow
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = PaginationDefaults.DefaultPageSize;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string[]? AllowedStatuses { get; set; }
    public int Total { get; set; }
    public int Filtered { get; set; }
    public IReadOnlyDictionary<string, int> Counts { get; set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, decimal> Amounts { get; set; } = new Dictionary<string, decimal>();
    public int AvailableCount { get; set; }
    public int Pages => Math.Max(1, (int)Math.Ceiling(Filtered / (double)Size));
    public void Normalize()
    {
        Page = Math.Clamp(Page, 1, 1000000);
        Size = PaginationDefaults.Normalize(Size);
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()[..Math.Min(Search.Trim().Length, 120)];
        Status = string.IsNullOrWhiteSpace(Status) ? null : Status.Trim();
    }
}
