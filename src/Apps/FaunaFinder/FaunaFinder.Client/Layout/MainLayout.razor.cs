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
    // The shell renders straight off the two managers' State, so subscribing is
    // the whole job — the handler body has nothing to add.
    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }

    [Event]
    private void OnNavbarChanged(NavbarChanged _) { }

    // Locale is cascaded to the tree, so it is the one thing a language flip
    // has to restate before the re-render the base already schedules.
    protected override void OnLanguageChanged() =>
        _locale = L.CurrentLanguage == "es" ? LocaleContext.Spanish : LocaleContext.English;
}
