using System.Collections.ObjectModel;
using FastEndpoints;
using FluentValidation;
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

    private static List<AbstractValidator<TRequest>>? _requestValidators;

    protected static void AddRequestValidator(AbstractValidator<TRequest> validator)
    {
        if (_requestValidators is null)
        {
            _requestValidators = new List<AbstractValidator<TRequest>>() { validator };
        }
        else
        {
            _requestValidators.Add(validator);
        }
    }

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
                var validationError = ValidateRequest(req);
                if (validationError is not null)
                {
                    await SendErrorResponseAsync(validationError);
                    return;
                }

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

    private ProblemDetailsResponse? ValidateRequest(TRequest req)
    {
        if (_requestValidators is null)
        {
            return null;
        }

        var errors = new Dictionary<string, object?>();

        foreach (var validator in _requestValidators)
        {
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                validationResult.Errors.ForEach(e =>
                {
                    if (errors.TryGetValue(e.PropertyName, out var errorList))
                    {
                        (errorList as List<string>)!.Add(e.ErrorMessage);
                    }
                    else
                    {
                        errors.Add(e.PropertyName, new List<string>() { e.ErrorMessage });
                    }
                });
            }
        }

        return errors.Count == 0
            ? null
            : new ProblemDetailsResponse
            {
                Type = "validation-error",
                StatusCode = 400,
                Title = "Validation has failed",
                Detail = "One or more errors occured",
                Extensions = new Dictionary<string, object?>() { { "errors", errors } },
            };
    }

    public override void Configure() { }
}
