using Microsoft.EntityFrameworkCore;
using Topstar.Application.Common;

namespace Topstar.Infrastructure.Persistence;

internal static class ListQueryExtensions
{
    public static async Task<List<T>> ToWindowAsync<T>(this IQueryable<T> source, ListWindow? window,
        CancellationToken ct, Func<IQueryable<T>, IQueryable<T>>? filter = null, System.Linq.Expressions.Expression<Func<T, string>>? status = null)
    {
        if (window is null) return await source.ToListAsync(ct);
        window.Normalize();
        window.Total = await source.CountAsync(ct);
        if (status is not null) window.Counts = await source.GroupBy(status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        if (filter is not null) source = filter(source);
        window.Filtered = await source.CountAsync(ct);
        window.Page = Math.Min(window.Page, window.Pages);
        return await source.Skip((window.Page - 1) * window.Size).Take(window.Size).ToListAsync(ct);
    }
}
