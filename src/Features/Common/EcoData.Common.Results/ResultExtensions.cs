namespace EcoData.Common.Results;

public static class ResultExtensions
{
    public static async Task<Result<TResult>> MapAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, TResult> mapper
    )
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    public static async Task<Result<TResult>> MapAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task<TResult>> mapper
    )
    {
        var result = await resultTask;
        if (result.IsFailure)
            return Result<TResult>.Failure(result.Error);

        var mapped = await mapper(result.Value);
        return Result<TResult>.Success(mapped);
    }

    public static async Task<Result<TResult>> BindAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Result<TResult>> binder
    )
    {
        var result = await resultTask;
        return result.Bind(binder);
    }

    public static async Task<Result<TResult>> BindAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task<Result<TResult>>> binder
    )
    {
        var result = await resultTask;
        return await result.BindAsync(binder);
    }

    public static async Task<TResult> MatchAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure
    )
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }

    public static async Task<TResult> MatchAsync<TValue, TResult>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task<TResult>> onSuccess,
        Func<Error, Task<TResult>> onFailure
    )
    {
        var result = await resultTask;
        return result.IsSuccess ? await onSuccess(result.Value) : await onFailure(result.Error);
    }

    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Action<TValue> action
    )
    {
        var result = await resultTask;
        return result.OnSuccess(action);
    }

    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task> action
    )
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value);

        return result;
    }

    public static async Task<Result<TValue>> OnFailureAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Action<Error> action
    )
    {
        var result = await resultTask;
        return result.OnFailure(action);
    }

    public static async Task<Result<TValue>> OnFailureAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Func<Error, Task> action
    )
    {
        var result = await resultTask;
        if (result.IsFailure)
            await action(result.Error);

        return result;
    }

    public static Result<TValue> ToResult<TValue>(this TValue? value, Error error)
        where TValue : class =>
        value is not null ? Result<TValue>.Success(value) : Result<TValue>.Failure(error);

    public static Result<TValue> ToResult<TValue>(this TValue? value, Error error)
        where TValue : struct =>
        value.HasValue ? Result<TValue>.Success(value.Value) : Result<TValue>.Failure(error);

    public static Result<TValue> ToResult<TValue>(this TValue? value, Func<Error> errorFactory)
        where TValue : class =>
        value is not null ? Result<TValue>.Success(value) : Result<TValue>.Failure(errorFactory());

    public static Result<TValue> ToResult<TValue>(this TValue? value, Func<Error> errorFactory)
        where TValue : struct =>
        value.HasValue
            ? Result<TValue>.Success(value.Value)
            : Result<TValue>.Failure(errorFactory());

    public static async Task<Result<TValue>> ToResultAsync<TValue>(
        this Task<TValue?> task,
        Error error
    )
        where TValue : class
    {
        var value = await task;
        return value.ToResult(error);
    }

    public static async Task<Result<TValue>> ToResultAsync<TValue>(
        this Task<TValue?> task,
        Func<Error> errorFactory
    )
        where TValue : class
    {
        var value = await task;
        return value.ToResult(errorFactory);
    }

    public static Result<IReadOnlyList<TValue>> Combine<TValue>(
        this IEnumerable<Result<TValue>> results
    )
    {
        var values = new List<TValue>();
        foreach (var result in results)
        {
            if (result.IsFailure)
                return Result<IReadOnlyList<TValue>>.Failure(result.Error);

            values.Add(result.Value);
        }

        return Result<IReadOnlyList<TValue>>.Success(values);
    }

    public static Result<(T1, T2)> Combine<T1, T2>(Result<T1> result1, Result<T2> result2)
    {
        if (result1.IsFailure)
            return result1.Error;
        if (result2.IsFailure)
            return result2.Error;

        return (result1.Value, result2.Value);
    }

    public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3
    )
    {
        if (result1.IsFailure)
            return result1.Error;
        if (result2.IsFailure)
            return result2.Error;
        if (result3.IsFailure)
            return result3.Error;

        return (result1.Value, result2.Value, result3.Value);
    }
}
