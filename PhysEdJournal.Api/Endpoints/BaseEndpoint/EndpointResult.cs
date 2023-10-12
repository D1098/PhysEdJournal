namespace PhysEdJournal.Api.Endpoints.BaseEndpoint;

public readonly struct EndpointResult<T>
{
    private readonly bool _isSuccess;
    private readonly T _value;
    private readonly ProblemDetailsResponse _error;

    public int StatusCode { get; }

    public EndpointResult(T value, int statusCode = 200)
    {
        _isSuccess = true;
        _error = default!;
        _value = value;
        StatusCode = statusCode;
    }

    public EndpointResult(ProblemDetailsResponse error)
    {
        _isSuccess = false;
        _error = error;
        _value = default!;
        StatusCode = error.Status;
    }

    public async Task MatchAsync(Func<T, Task> success, Func<ProblemDetailsResponse, Task> failure)
    {
        if (_isSuccess)
        {
            await success(_value);
        }
        else
        {
            await failure(_error);
        }
    }

    public static EndpointResult<T> Create(T value, int code = 200) => new(value, code);

    public static EndpointResult<T> Create(ProblemDetailsResponse error) => new(error);

    public static implicit operator EndpointResult<T>(T value) => new(value);

    public static implicit operator EndpointResult<T>(ProblemDetailsResponse error) => new(error);
}
