using Microsoft.EntityFrameworkCore;

namespace TrilobitCS.Queries;

public interface IQueryOptions<TEntity> where TEntity : class
{
    string[] AllowedFilters();
    string[] AllowedSorts();
    string[] AllowedIncludes();
    IOrderedQueryable<TEntity> ApplyDefaultSort(IQueryable<TEntity> query)
        => query.OrderBy(e => EF.Property<int>(e, "Id"));
}
