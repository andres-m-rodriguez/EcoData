using EcoData.Ui.Interop;
using Microsoft.JSInterop;
using Tempest;

namespace FaunaFinder.Client.Services.Theme;

// The light/dark choice, shared by the desktop toggle and the phone settings
// panel. The layout's theme provider reads IsDark back on every Changed.
public sealed class ThemePreference(IJavascriptSafeInterop js, IEventBus bus)
{
    public sealed record Changed;

    private IJSObjectReference? _module;

    public bool IsDark { get; private set; }

    // Call from OnAfterRenderAsync: the import needs a document. A failed
    // import leaves the stamped theme alone and the switches do nothing.
    public async Task InitializeAsync()
    {
        if (_module is not null)
            return;

        var imported = await js.ImportAsync("./js/fauna-theme.js");
        if (!imported.TryPickT0(out var module, out _))
            return;

        _module = module;

        var theme = await js.InvokeAsync<string>(module, "getTheme");
        IsDark = theme.TryPickT0(out var name, out _)
            && string.Equals(name, "dark", StringComparison.OrdinalIgnoreCase);
        bus.Publish<Changed>();
    }

    public async Task SetAsync(bool dark)
    {
        if (_module is null)
            return;

        var stamped = await js.InvokeVoidAsync(_module, "setTheme", dark ? "dark" : "light");
        if (!stamped.IsT0)
            return;

        IsDark = dark;
        bus.Publish<Changed>();
    }
}
