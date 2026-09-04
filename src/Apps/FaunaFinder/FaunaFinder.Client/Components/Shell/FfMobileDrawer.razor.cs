using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Layout;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handlers live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfMobileDrawer : EcoDataComponent
{
    // Published by the app bar's panel glyph.
    public sealed record Toggle;

    private bool _open;

    [Event]
    private void OnToggle(Toggle _)
    {
        if (_open)
        {
            Close();
            return;
        }

        _open = true;
        Bus.Publish<MainLayout.TopBarHidden>();
    }

    [Event]
    private void OnNavigationChanged(NavigationChanged _)
    {
        if (_open)
            Close();
    }

    private void Close()
    {
        _open = false;
        Bus.Publish<MainLayout.TopBarShown>();
    }
}
