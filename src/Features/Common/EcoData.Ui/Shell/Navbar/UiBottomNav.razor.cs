using EcoData.Spa.Navigation.Events;
using Microsoft.AspNetCore.Components;
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

    // Transient: this bar owns its watcher for as long as it is on screen.
    [Inject]
    private NavAutoHide AutoHide { get; set; } = default!;

    private bool _hidden;

    private string BarClass => _hidden ? "ui-bottom-nav is-hidden" : "ui-bottom-nav";

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
        GC.SuppressFinalize(this);
    }

    [Event]
    private void OnHidden(Hidden _) => _hidden = true;

    [Event]
    private void OnShown(Shown _) => _hidden = false;

    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }
}
