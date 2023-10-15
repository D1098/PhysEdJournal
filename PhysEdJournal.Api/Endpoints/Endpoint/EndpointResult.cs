namespace PhysEdJournal.Api.Endpoints.Endpoint;

public readonly struct EndpointResult<T>
{
    private readonly T _value;
    private readonly ProblemDetailsResponse _error;

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public EndpointResult(T value, int statusCode = 200)
    {
        IsSuccess = true;
        _error = default!;
        _value = value;
        StatusCode = statusCode;
    }

    public EndpointResult(ProblemDetailsResponse error)
    {
        IsSuccess = false;
        _error = error;
        _value = default!;
        StatusCode = error.StatusCode;
    }

    public T UnsafeGet()
    {
        if (!IsSuccess)
        {
            throw new Exception("Trying to access value in Faulted state");
        }

        return _value;
    }

    public async Task MatchAsync(Func<T, Task> success, Func<ProblemDetailsResponse, Task> failure)
    {
        if (IsSuccess)
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
