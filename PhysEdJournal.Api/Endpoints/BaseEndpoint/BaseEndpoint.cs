using FastEndpoints;
using Serilog;

namespace PhysEdJournal.Api.Endpoints.BaseEndpoint;

public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    public required IDiagnosticContext DiagnosticContext { get; set; }

    public sealed override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var result = await ExecuteCommand(req);

        await result.MatchAsync(
            async response => await SendAsync(response, result.StatusCode, ct),
            async errorResponse => await SendResultAsync(Results.Json(errorResponse))
        );
    }

    protected abstract Task<EndpointResult<TResponse>> ExecuteCommand(TRequest request);
}
