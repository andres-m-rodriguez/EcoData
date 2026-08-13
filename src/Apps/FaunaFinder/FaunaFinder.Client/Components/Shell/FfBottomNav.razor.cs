using EcoData.NativeUi;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handlers live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that LocalizedComponentBase is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfBottomNav : LocalizedComponentBase
{
    /// <summary>
    /// Get the tab bar out of the way — published by a screen that needs the
    /// bottom of the viewport, such as the map's municipality sheet.
    ///
    /// <para>Nested here on purpose: <c>[Event]</c> only binds to a record
    /// declared on the handling component. Publishers elsewhere say
    /// <c>Bus.Publish&lt;FfBottomNav.Hidden&gt;()</c>.</para>
    /// </summary>
    public sealed record Hidden;

    /// <summary>Give the tab bar back.</summary>
    public sealed record Shown;

    private bool _hidden;
    private NavigationTab _currentTab = NavigationTab.Map;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The scroll watcher for the mobile chrome — this bar and the app bar
    /// both. It hides them by stamping an attribute on the root element, so
    /// nothing here re-renders on scroll; a bar that asked Blazor to re-render
    /// every frame would cost more than it is worth.
    ///
    /// <para>It lives on this component rather than the layout because this
    /// one renders on small screens only, so the watcher exists exactly when
    /// the bars it drives do — and the definition of "phone" stays in the
    /// markup and the stylesheets rather than being restated in JS.</para>
    /// </summary>
    private IJSObjectReference? _autoHide;

    private string BarClass => _hidden ? "bottom-nav is-hidden" : "bottom-nav";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navigation.OnStateChanged += HandleNavigationStateChanged;
        UpdateCurrentTab();
    }

    // This is a WebAssembly client, so the module import is async and can only
    // run once there's a document to talk to — never from OnInitialized. The
    // bar renders on small screens only, so on a desktop this never runs and
    // no listener is attached.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        try
        {
            _autoHide = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/fauna-nav-autohide.js"
            );

            await _autoHide.InvokeVoidAsync("start");
        }
        catch (JSException)
        {
            // Auto-hiding is an enhancement, not a dependency: if the module
            // can't load, the bar simply stays put.
            _autoHide = null;
        }
    }

    public override void Dispose()
    {
        Navigation.OnStateChanged -= HandleNavigationStateChanged;

        if (_autoHide is not null)
        {
            // Dispose is synchronous, so this can only be started, not awaited.
            // The module clears the attribute itself, so the bar is never left
            // hidden by a watcher that has gone away.
            _ = StopAutoHideAsync(_autoHide);
            _autoHide = null;
        }

        base.Dispose();
    }

    private static async Task StopAutoHideAsync(IJSObjectReference module)
    {
        try
        {
            await module.InvokeVoidAsync("stop");
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit or page is already gone; there is nothing to clean up.
        }
        catch (JSException)
        {
            // Same: teardown is best effort.
        }
    }

    [Event]
    private void OnHidden(Hidden _) => _hidden = true;

    [Event]
    private void OnShown(Shown _) => _hidden = false;

    private void HandleNavigationStateChanged()
    {
        UpdateCurrentTab();
        InvokeAsync(StateHasChanged);
    }

    private void UpdateCurrentTab()
    {
        var path = Navigation.State.Path;
        _currentTab = path switch
        {
            "/" or "" => NavigationTab.Map,
            var p when p.StartsWith("/map", StringComparison.OrdinalIgnoreCase) => NavigationTab.Map,
            var p when p.StartsWith("/species", StringComparison.OrdinalIgnoreCase) => NavigationTab.Species,
            var p when p.StartsWith("/municipalities", StringComparison.OrdinalIgnoreCase) => NavigationTab.Municipalities,
            // Every reference section lights the one Browse tab.
            var p when p.StartsWith("/browse", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/categories", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/practices", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/actions", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            _ => NavigationTab.Map
        };
    }

    private Color TabColor(NavigationTab tab) =>
        _currentTab == tab ? Color.Primary : Color.Default;

    private string TabClass(NavigationTab tab) =>
        _currentTab == tab ? "bottom-nav-item is-active" : "bottom-nav-item";

    private void GoTo(NavigationTab tab)
    {
        var path = tab switch
        {
            NavigationTab.Species => "/species",
            NavigationTab.Municipalities => "/municipalities",
            NavigationTab.Browse => "/browse",
            _ => "/"
        };
        Navigation.NavigateTo(path);
    }

    private enum NavigationTab { Map, Species, Municipalities, Browse }
}
