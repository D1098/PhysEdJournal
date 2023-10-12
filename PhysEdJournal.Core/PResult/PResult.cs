﻿namespace PhysEdJournal.Core.PResult;

public readonly struct PResult<TValue> : IEquatable<PResult<TValue>>
{
    private enum ResultState
    {
        Success,
        Failure,
    }

    private readonly ResultState _state;
    private readonly TValue _value;
    private readonly Exception _error;

    public PResult(TValue value)
    {
        _state = ResultState.Success;
        _value = value;
        _error = default!;
    }

    public PResult(Exception error)
    {
        _state = ResultState.Failure;
        _value = default!;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public bool IsSuccess => _state == ResultState.Success;
    public bool IsError => _state == ResultState.Failure;

    public TRes Match<TRes>(Func<TValue, TRes> success, Func<Exception, TRes> fail) =>
        IsSuccess ? success(_value) : fail(_error);

    public TValue UnsafeValue =>
        IsError ? throw new ArgumentException("Trying to access value in Failure state") : _value;

    public Exception UnsafeError =>
        IsSuccess ? throw new ArgumentException("Trying to access error in Success state") : _error;

    public static implicit operator PResult<TValue>(TValue value) => new(value);

    public static implicit operator PResult<TValue>(Exception error) => new(error);

    public bool Equals(PResult<TValue> other)
    {
        return _state == other._state
            && EqualityComparer<TValue>.Default.Equals(_value, other._value)
            && _error.Equals(other._error);
    }

    public override bool Equals(object? obj)
    {
        return obj is PResult<TValue> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)_state, _value, _error);
    }

    public static bool operator ==(PResult<TValue> left, PResult<TValue> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PResult<TValue> left, PResult<TValue> right)
    {
        return !(left == right);
    }
}