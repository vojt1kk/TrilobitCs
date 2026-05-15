using Microsoft.EntityFrameworkCore;

namespace TrilobitCS.Pagination;

public static class PaginationExtensions
{
    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        PaginationQuery pagination,
        Func<TSource, TResult> selector,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var rawItems = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);
        var items = rawItems.Select(selector).ToList();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pagination.PageSize);
        return new PagedResponse<TResult>(items, pagination.Page, pagination.PageSize, totalCount, totalPages);
    }
}
