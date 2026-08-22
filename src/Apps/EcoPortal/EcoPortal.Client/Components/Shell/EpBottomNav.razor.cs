using EcoData.Spa.Navigation.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Tempest;

namespace EcoPortal.Client.Components.Shell;

public partial class EpBottomNav : StatefulComponent
{
    public sealed record Hidden;
    public sealed record Shown;
    private bool _hidden;
    [Inject]
    private IJSRuntime JS { get; set; } = default!;
    private IJSObjectReference? _autoHide;

    private string BarClass => _hidden ? "bottom-nav is-hidden" : "bottom-nav";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        try
        {
            _autoHide = await JS.InvokeAsync<IJSObjectReference>("import", "./js/eco-nav-autohide.js");
            await _autoHide.InvokeVoidAsync("start");
        }
        catch (JSException)
        {
            _autoHide = null;
        }
    }

    public override void Dispose()
    {
        if (_autoHide is not null)
        {
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
        }
        catch (JSException)
        {
        }
    }
    [Event]
    private void OnHidden(Hidden _) => _hidden = true;
    [Event]
    private void OnShown(Shown _) => _hidden = false;
    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }
}
