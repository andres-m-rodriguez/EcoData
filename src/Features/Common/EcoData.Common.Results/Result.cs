using System.Diagnostics.CodeAnalysis;

namespace EcoData.Common.Results;

public readonly struct Result<TValue> : IEquatable<Result<TValue>>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => !IsSuccess;

    public TValue Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException(
                $"Cannot access Value on a failed Result. Error: {_error}"
            );

    public Error Error =>
        IsFailure
            ? _error
            : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure
    ) => IsSuccess ? onSuccess(_value) : onFailure(_error);

    public void Switch(Action<TValue> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value);
        else
            onFailure(_error);
    }

    public Result<TResult> Map<TResult>(Func<TValue, TResult> mapper) =>
        IsSuccess ? Result<TResult>.Success(mapper(_value)) : Result<TResult>.Failure(_error);

    public Result<TResult> Bind<TResult>(Func<TValue, Result<TResult>> binder) =>
        IsSuccess ? binder(_value) : Result<TResult>.Failure(_error);

    public async Task<Result<TResult>> BindAsync<TResult>(
        Func<TValue, Task<Result<TResult>>> binder
    ) => IsSuccess ? await binder(_value) : Result<TResult>.Failure(_error);

    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
            action(_value);

        return this;
    }

    public Result<TValue> OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(_error);

        return this;
    }

    public TValue GetValueOrDefault(TValue defaultValue) => IsSuccess ? _value : defaultValue;

    public TValue GetValueOrDefault(Func<Error, TValue> defaultFactory) =>
        IsSuccess ? _value : defaultFactory(_error);

    public bool Equals(Result<TValue> other)
    {
        if (IsSuccess != other.IsSuccess)
            return false;

        return IsSuccess
            ? EqualityComparer<TValue>.Default.Equals(_value, other._value)
            : _error!.Equals(other._error);
    }

    public override bool Equals(object? obj) => obj is Result<TValue> other && Equals(other);

    public override int GetHashCode() =>
        IsSuccess ? _value?.GetHashCode() ?? 0 : _error!.GetHashCode();

    public static bool operator ==(Result<TValue> left, Result<TValue> right) => left.Equals(right);

    public static bool operator !=(Result<TValue> left, Result<TValue> right) =>
        !left.Equals(right);

    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}

public static class Result
{
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);

    public static Result<Unit> Success() => Result<Unit>.Success(Unit.Value);

    public static Result<Unit> Failure(Error error) => Result<Unit>.Failure(error);
}

public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj) => obj is Unit;

    public override int GetHashCode() => 0;

    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right) => true;

    public static bool operator !=(Unit left, Unit right) => false;
}
