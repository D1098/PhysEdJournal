namespace PhysEdJournal.Api.Endpoints.Endpoint.Pagination;

public abstract class OffsetPaginationQueryEndpoint<TRequest, TResponse>
    : QueryEndpoint<TRequest, TResponse>
    where TRequest : PaginationRequest
    where TResponse : PaginationResponse
{
    protected sealed override async Task<EndpointResult<TResponse>> ExecuteCommandAsync(
        TRequest request,
        CancellationToken ct = default
    )
    {
        var result = await ExecutePagingCommandAsync(request, ct);

        if (result.IsSuccess)
        {
            var resp = result.UnsafeGet();
            HttpContext.Response.Headers.Add("X-TotalCount", resp.TotalCount.ToString());
        }

        return result;
    }

    protected abstract Task<EndpointResult<TResponse>> ExecutePagingCommandAsync(
        TRequest request,
        CancellationToken ct = default
    );
}
