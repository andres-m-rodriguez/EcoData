using Microsoft.JSInterop;
using OneOf;
using OneOf.Types;

namespace EcoData.Ui.Interop;

// JS interop that never throws at the call site. Every failure a JS call can
// produce — a script error, a gone browser, interop not being available yet,
// cancellation — comes back as a JsFailure the caller can branch on or ignore,
// so "this is an enhancement, not a dependency" is one TryPick, not a try/catch.
public interface IJavascriptSafeInterop
{
    // Imports an ES module, e.g. "./_content/EcoData.Ui/js/nav-autohide.js".
    ValueTask<OneOf<IJSObjectReference, JsFailure>> ImportAsync(
        string modulePath,
        CancellationToken cancellationToken = default
    );

    ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(
        IJSObjectReference module,
        string identifier,
        params object?[] args
    );

    ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(
        IJSObjectReference module,
        string identifier,
        params object?[] args
    );

    // Global (window-level) functions, for scripts that are not modules.
    ValueTask<OneOf<Success, JsFailure>> InvokeVoidAsync(string identifier, params object?[] args);

    ValueTask<OneOf<TResult, JsFailure>> InvokeAsync<TResult>(string identifier, params object?[] args);

    // Releases a module reference. Failing to release is never worth reporting
    // beyond the result: the browser is usually already gone when it fails.
    ValueTask<OneOf<Success, JsFailure>> DisposeAsync(IJSObjectReference module);
}
