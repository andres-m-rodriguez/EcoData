using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation;
using EcoData.Spa.Navigation.Events;
using EcoData.Ui.Shell.Navbar;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handlers live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfBottomNav : EcoDataComponent
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

    // The scroll watcher for the mobile chrome — this bar and the app bar both.
    // It lives on this component rather than the layout because this one
    // renders on small screens only, so the watcher exists exactly when the
    // bars it drives do. Transient, so the bar owns its own.
    [Inject]
    private NavAutoHide AutoHide { get; set; } = default!;

    private string BarClass => _hidden ? "bottom-nav is-hidden" : "bottom-nav";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateCurrentTab(Navigation.State.Path);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await AutoHide.StartAsync();
        }
    }

    public override void Dispose()
    {
        // Dispose is synchronous, so this can only be started, not awaited.
        _ = AutoHide.DisposeAsync().AsTask();
        base.Dispose();
    }

    [Event]
    private void OnHidden(Hidden _) => _hidden = true;

    [Event]
    private void OnShown(Shown _) => _hidden = false;

    [Event]
    private void OnNavigationChanged(NavigationChanged e) => UpdateCurrentTab(e.State.Path);

    private void UpdateCurrentTab(string path)
    {
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
