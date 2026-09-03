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
    public sealed record Hidden;

    public sealed record Shown;

    private bool _hidden;
    private NavigationTab _currentTab = NavigationTab.Map;

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
            await AutoHide.StartAsync();
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
            var p when p.StartsWith("/coordinates", StringComparison.OrdinalIgnoreCase) => NavigationTab.Map,
            var p when p.StartsWith("/species", StringComparison.OrdinalIgnoreCase) => NavigationTab.Species,
            var p when p.StartsWith("/municipalities", StringComparison.OrdinalIgnoreCase) => NavigationTab.Municipalities,
            var p when p.StartsWith("/browse", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/categories", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/practices", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/actions", StringComparison.OrdinalIgnoreCase) => NavigationTab.Browse,
            var p when p.StartsWith("/account", StringComparison.OrdinalIgnoreCase) => NavigationTab.Account,
            var p when p.StartsWith("/login", StringComparison.OrdinalIgnoreCase) => NavigationTab.Account,
            var p when p.StartsWith("/register", StringComparison.OrdinalIgnoreCase) => NavigationTab.Account,
            var p when p.StartsWith("/sightings", StringComparison.OrdinalIgnoreCase) => NavigationTab.Account,
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
            NavigationTab.Account => "/account",
            _ => "/"
        };
        Navigation.NavigateTo(path);
    }

    private enum NavigationTab { Map, Species, Municipalities, Browse, Account }
}
