using System.Collections.ObjectModel;
using FastEndpoints;
using FluentValidation;
using Serilog;
using Serilog.Context;

namespace PhysEdJournal.Api.Endpoints.Common;

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
    /// <summary>
    /// Контекст, используемый для передачи дополнительных параметров в результирующий лог,
    /// который будет записан по окончанию запроса.
    /// </summary>
    public required IDiagnosticContext DiagnosticContext { get; init; }

    private static List<AbstractValidator<TRequest>>? _requestValidators;

    /// <summary>
    /// Метод добавления валидотора запроса.
    /// Поддерживает несколько валидаторов одновременно.
    /// Если добавили больше 1 валидатора, то все они отработают, и в случае завершения хотя бы 1 с ошибкой,
    /// запрос завершится в ответе будут собраны все ошибки всех валидаторов.
    /// </summary>
    /// <param name="validator">Валидатор запроса из библиотеки FluentValidation.</param>
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

    /// <summary>
    /// Хэндлер эндпоинта, вызывается для обработки каждого запроса.
    /// </summary>
    /// <param name="request">Объект запроса.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns><see cref="EndpointResult{T}"/>.</returns>
    protected abstract Task<EndpointResult<TResponse>> ExecuteCommandAsync(
        TRequest request,
        CancellationToken ct = default
    );

    /// <summary>
    /// Используется, если нужно выполнить какую-то логику перед запросом
    /// и, возможно, прервать запрос до выполнения команды.
    /// </summary>
    /// <param name="req">Объект запроса.</param>
    /// <returns><see cref="ProblemDetailsResponse"/> или null, если все впорядке.</returns>
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
