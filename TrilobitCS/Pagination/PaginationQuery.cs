namespace TrilobitCS.Pagination;

public class PaginationQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public const int MaxPageSize = 100;
}
