using Microsoft.JSInterop;
using OneOf;
using OneOf.Types;

namespace EcoData.Ui.Interop;

public sealed class JavascriptSafeInterop(IJSRuntime js) : IJavascriptSafeInterop
{
    public ValueTask<OneOf<IJSObjectReference, JsFailure>> ImportAsync(
        string modulePath,
        CancellationToken cancellationToken = default
    ) => GuardAsync(async () => await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, modulePath));

    public ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(
        IJSObjectReference module,
        string identifier,
        params object?[] args
    ) => GuardVoidAsync(async () => await module.InvokeVoidAsync(identifier, args));

    public ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(
        IJSObjectReference module,
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args
    ) => GuardVoidAsync(async () => await module.InvokeVoidAsync(identifier, cancellationToken, args));

    public ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(
        IJSObjectReference module,
        string identifier,
        params object?[] args
    ) => GuardAsync(async () => await module.InvokeAsync<TResult>(identifier, args));

    public ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(
        IJSObjectReference module,
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args
    ) => GuardAsync(async () => await module.InvokeAsync<TResult>(identifier, cancellationToken, args));

    public ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(string identifier, params object?[] args) =>
        GuardVoidAsync(async () => await js.InvokeVoidAsync(identifier, args));

    public ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args
    ) => GuardVoidAsync(async () => await js.InvokeVoidAsync(identifier, cancellationToken, args));

    public ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(string identifier, params object?[] args) =>
        GuardAsync(async () => await js.InvokeAsync<TResult>(identifier, args));

    public ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args
    ) => GuardAsync(async () => await js.InvokeAsync<TResult>(identifier, cancellationToken, args));

    public ValueTask<OneOf<Success, JsFailure>> DisposeAsync(IJSObjectReference module) =>
        GuardVoidAsync(async () => await module.DisposeAsync());

    private static async ValueTask<OneOf<Success, JsFailure>> GuardVoidAsync(Func<Task> call)
    {
        try
        {
            await call();
            return new Success();
        }
        catch (Exception e) when (Classify(e) is { } failure)
        {
            return failure;
        }
    }

    private static async ValueTask<OneOf<T, JsFailure>> GuardAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception e) when (Classify(e) is { } failure)
        {
            return failure;
        }
    }

    // Only what JS interop itself can throw is turned into a result. Anything
    // else is a bug in the caller and keeps propagating.
    private static JsFailure? Classify(Exception e) => e switch
    {
        JSDisconnectedException => new JsFailure(JsFailureKind.Disconnected, e.Message),
        JSException => new JsFailure(JsFailureKind.ScriptError, e.Message),
        OperationCanceledException => new JsFailure(JsFailureKind.Cancelled, e.Message),
        InvalidOperationException => new JsFailure(JsFailureKind.Unavailable, e.Message),
        _ => null,
    };
}
