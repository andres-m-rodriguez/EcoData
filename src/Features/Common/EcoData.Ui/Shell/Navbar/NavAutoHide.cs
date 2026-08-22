using Microsoft.JSInterop;

namespace EcoData.Ui.Shell.Navbar;

// The scroll watcher behind the mobile chrome: nav-autohide.js stamps
// data-nav-autohide on the root element while the reader heads down the page,
// and the app bar and bottom nav both read it. One instance per bar that
// renders on small screens, so the watcher exists exactly when the bars do.
public sealed class NavAutoHide(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    // A WebAssembly import can only run once there is a document — call from
    // OnAfterRenderAsync, never OnInitialized. Failure to load leaves the bars
    // in place: auto-hiding is an enhancement, not a dependency.
    public async Task StartAsync()
    {
        if (_module is not null)
        {
            return;
        }

        try
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/EcoData.Ui/js/nav-autohide.js");
            await _module.InvokeVoidAsync("start");
        }
        catch (JSException)
        {
            _module = null;
        }
    }

    // The module clears the attribute itself, so a bar is never left hidden by
    // a watcher that has gone away.
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        var module = _module;
        _module = null;

        try
        {
            await module.InvokeVoidAsync("stop");
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
    }
}
