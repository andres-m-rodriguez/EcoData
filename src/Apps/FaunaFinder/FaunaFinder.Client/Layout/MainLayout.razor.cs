using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Localization;
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

    // The shell renders straight off the managers' State; the phone drawer is
    // the one thing a navigation has to put away.
    [Event]
    private void OnNavigationChanged(NavigationChanged _) => _drawerOpen = false;

    [Event]
    private void OnNavbarChanged(NavbarChanged _) { }

    protected override void OnLanguageChanged() =>
        _locale = L.CurrentLanguage == "es" ? LocaleContext.Spanish : LocaleContext.English;
}
