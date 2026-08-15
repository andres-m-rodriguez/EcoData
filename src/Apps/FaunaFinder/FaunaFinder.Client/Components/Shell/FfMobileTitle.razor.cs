using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Localization;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handler lives in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that LocalizedComponentBase is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfMobileTitle : LocalizedComponentBase
{
    /// <summary>
    /// A portrait to show beside the mobile page title — the species you have
    /// open, the way a messaging app shows who you are talking to.
    ///
    /// <para>Publish with a null <paramref name="Src"/> to show none. Nested
    /// here because <c>[Event]</c> only binds to a record declared on the
    /// handling component; pages say
    /// <c>Bus.Publish(new FfMobileTitle.Avatar(src, alt))</c>.</para>
    /// </summary>
    /// <param name="Src">Image URL, or null for no portrait.</param>
    /// <param name="Alt">
    /// Alternative text. Empty is correct when the title beside it already
    /// names the same thing — otherwise a screen reader hears it twice.
    /// </param>
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

    // Dropping the portrait here rather than asking pages to clear it on the
    // way out: a page that forgets would leave its animal sitting over the
    // next screen's title.
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
