using EcoData.Spa.Navigation.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Tempest;

namespace EcoData.Ui.Shell.Navbar;

public partial class UiBottomNav : StatefulComponent
{
    // Nested because [Event] only binds to a record declared on the handling
    // component: Bus.Publish<UiBottomNav.Hidden>().
    public sealed record Hidden;

    public sealed record Shown;

    [Parameter, EditorRequired]
    public required IReadOnlyList<UiBottomNavItem> Items { get; set; }

    [Parameter]
    public string? ActiveKey { get; set; }

    [Parameter]
    public EventCallback<string> OnSelect { get; set; }

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private bool _hidden;
    private NavAutoHide? _autoHide;

    private string BarClass => _hidden ? "ui-bottom-nav is-hidden" : "ui-bottom-nav";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        _autoHide = new NavAutoHide(JS);
        await _autoHide.StartAsync();
    }

    public override void Dispose()
    {
        if (_autoHide is not null)
        {
            // Dispose is synchronous, so this can only be started, not awaited.
            _ = _autoHide.DisposeAsync();
            _autoHide = null;
        }

        base.Dispose();
        GC.SuppressFinalize(this);
    }

    [Event]
    private void OnHidden(Hidden _) => _hidden = true;

    [Event]
    private void OnShown(Shown _) => _hidden = false;

    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }
}
