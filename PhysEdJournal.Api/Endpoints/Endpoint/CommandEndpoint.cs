namespace PhysEdJournal.Api.Endpoints.Endpoint;

public abstract class CommandEndpoint<TRequest, TResponse> : BaseEndpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected override EndpointType EndpointType => EndpointType.Command;
}
