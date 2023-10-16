using Newtonsoft.Json;
using PhysEdJournal.Api.Endpoints.Endpoint.Pagination;

namespace PhysEdJournal.Api.Endpoints.Endpoint;

public class Req : PaginationRequest
{
    public required string Name { get; init; }
}

public class Res : PaginationResponse
{
    public required string Success { get; init; }
}

public class Test : OffsetPaginationQueryEndpoint<Req, Res>
{
    public override void Configure()
    {
        base.Configure();
        AllowAnonymous();
        Get("test");
    }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected override async Task<EndpointResult<Res>> ExecutePagingCommandAsync(
        Req request,
        CancellationToken ct = default
    )
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Console.WriteLine(JsonConvert.SerializeObject(request));

        return new Res { Success = "true", TotalCount = 10, };
    }
}
