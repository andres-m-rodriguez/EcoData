using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Localization;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handler lives in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfMobileTitle : EcoDataComponent
{
    public sealed record Avatar(string? Src, string? Alt);

    private string? _avatarSrc;
    private string? _avatarAlt;

    private bool IsRootPage =>
        Navigation.State.Path is "/" or "";

    [Event]
    private void OnAvatar(Avatar avatar)
    {
        _avatarSrc = avatar.Src;
        _avatarAlt = avatar.Alt;
    }

    [Event]
    private void OnNavigationChanged(NavigationChanged _)
    {
        _avatarSrc = null;
        _avatarAlt = null;
    }

    // The title renders straight off Navbar.State, so subscribing is the whole job.
    [Event]
    private void OnNavbarChanged(NavbarChanged _) { }
}
