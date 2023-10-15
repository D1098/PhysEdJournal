namespace PhysEdJournal.Api.Endpoints.Endpoint;

public abstract class QueryEndpoint<TRequest, TResponse> : BaseEndpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected sealed override EndpointType EndpointType { get; init; } = EndpointType.Query;
}
