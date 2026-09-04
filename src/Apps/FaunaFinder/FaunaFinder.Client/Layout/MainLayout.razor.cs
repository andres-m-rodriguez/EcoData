using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.Theme;
using Tempest;

namespace FaunaFinder.Client.Layout;

// The [Event] handlers live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataLayout is a StatefulLayoutComponent;
// the C# symbol frontend can.
public partial class MainLayout : EcoDataLayout
{
    public sealed record AuthChanged;

    // Published by the review page after every approve, deny or unapprove so
    // the account page's pending count follows the decision.
    public sealed record SightingsReviewed;

    // The shell renders straight off the two managers' State, so subscribing is
    // the whole job — the handler body has nothing to add.
    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }

    // Published by whatever needs the top bar out of the way, the phone drawer
    // for one; the bar slides off the same way the scroll auto-hide takes it.
    public sealed record TopBarHidden;

    public sealed record TopBarShown;

    [Event]
    private void OnTopBarHidden(TopBarHidden _) => _topBarHidden = true;

    [Event]
    private void OnTopBarShown(TopBarShown _) => _topBarHidden = false;

    // The theme provider reads Theme.IsDark on render; subscribing is the whole job.
    [Event]
    private void OnThemeChanged(ThemePreference.Changed _) { }

    [Event]
    private void OnNavbarChanged(NavbarChanged _) { }

    protected override void OnLanguageChanged() =>
        _locale = L.CurrentLanguage == "es" ? LocaleContext.Spanish : LocaleContext.English;
}
