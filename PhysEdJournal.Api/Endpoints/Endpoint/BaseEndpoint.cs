﻿using System.Collections.ObjectModel;
using FastEndpoints;
using Serilog;
using Serilog.Context;

namespace PhysEdJournal.Api.Endpoints.Endpoint;

internal static class OperationToString
{
    internal static ReadOnlyDictionary<EndpointType, string> Stringify { get; } =
        new Dictionary<EndpointType, string>
        {
            { EndpointType.Query, "Query" },
            { EndpointType.Command, "Command" },
        }.AsReadOnly();
}

public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    public required IDiagnosticContext DiagnosticContext { get; init; }

    protected abstract EndpointType EndpointType { get; init; }

    public sealed override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var userClaim = User.FindFirst("IndividualGuid");

        DiagnosticContext.Set("UserGuid", userClaim?.Value);
        DiagnosticContext.Set("OperationType", OperationToString.Stringify[EndpointType]);
        DiagnosticContext.Set("Args", req);

        try
        {
            using (LogContext.PushProperty("UserGuid", userClaim?.Value))
            {
                var err = await BeforeCommandExecuteAsync(req);
                if (err is not null)
                {
                    await SendErrorResponseAsync(err);
                    return;
                }

                var result = await ExecuteCommandAsync(req, ct);

                await result.MatchAsync(
                    async response => await SendAsync(response, result.StatusCode, ct),
                    async error => await SendErrorResponseAsync(error)
                );
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unhandled exception");
            await SendResultAsync(
                Results.Json(
                    new ProblemDetailsResponse
                    {
                        Type = "unexpected-error",
                        StatusCode = 500,
                        Title = "Server error",
                        Detail = "Something bad happened during request execution",
                    },
                    statusCode: 500
                )
            );
        }
    }

    protected abstract Task<EndpointResult<TResponse>> ExecuteCommandAsync(
        TRequest request,
        CancellationToken ct = default
    );

    protected virtual Task<ProblemDetailsResponse?> BeforeCommandExecuteAsync(TRequest req)
    {
        return Task.FromResult<ProblemDetailsResponse?>(null);
    }

    private async Task SendErrorResponseAsync(ProblemDetailsResponse err)
    {
        await SendResultAsync(Results.Json(err, statusCode: err.StatusCode));
    }
}
