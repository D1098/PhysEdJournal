namespace PhysEdJournal.Api.Endpoints.Endpoint.Pagination;

public class PaginationRequest
{
    public const int MaxPageSize = 200;
    private int _pageSize = 40;

    /// <summary>
    /// Page number.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Amount of data you need on single page. Must be greater than 0.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
