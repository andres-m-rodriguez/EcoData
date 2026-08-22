using EcoData.Ui.Interop;
using Microsoft.JSInterop;

namespace EcoData.Ui.Shell.Navbar;

// The scroll watcher behind the mobile chrome: nav-autohide.js stamps
// data-nav-autohide on the root element while the reader heads down the page,
// and the app bar and bottom nav both read it. One instance per bar that
// renders on small screens, so the watcher exists exactly when the bars do.
public sealed class NavAutoHide(IJavascriptSafeInterop js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/EcoData.Ui/js/nav-autohide.js";

    private IJSObjectReference? _module;

    // Call from OnAfterRenderAsync — a WebAssembly import needs a document.
    // Auto-hiding is an enhancement, not a dependency: a failed import leaves
    // the bars in place and there is nothing for the caller to handle.
    public async Task StartAsync()
    {
        if (_module is not null)
            return;

        var imported = await js.ImportAsync(ModulePath);
        if (!imported.TryPickT0(out var module, out _))
            return;

        _module = module;
        await js.InvokeVoidAsync(module, "start");
    }

    // The module clears the attribute itself, so a bar is never left hidden by
    // a watcher that has gone away. A failure here means the browser is already
    // gone; there is nothing left to clean up.
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        var module = _module;
        _module = null;

        await js.InvokeVoidAsync(module, "stop");
        await js.DisposeAsync(module);
    }
}
