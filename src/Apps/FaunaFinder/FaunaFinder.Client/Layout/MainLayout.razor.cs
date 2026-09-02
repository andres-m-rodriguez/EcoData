using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using EcoData.Wildlife.Application.Client;
using EcoData.Wildlife.Contracts;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
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
    // the pending badge follows the decision.
    public sealed record SightingsReviewed;

    [Inject]
    private ISightingHttpClient SightingClient { get; set; } = default!;

    // The shell renders straight off the two managers' State, so subscribing is
    // the whole job — the handler body has nothing to add.
    [Event]
    private void OnNavigationChanged(NavigationChanged _) { }

    [Event]
    private void OnNavbarChanged(NavbarChanged _) { }

    [Event]
    private void OnAuthChanged(AuthChanged _) => LoadPendingCountState.TryExecute();

    [Event]
    private void OnSightingsReviewed(SightingsReviewed _) => LoadPendingCountState.TryExecute();

    // Zero whenever the account can't review: the badge hides and no request
    // leaves the browser.
    [Command]
    private async Task<int> LoadPendingCount(CancellationToken ct)
    {
        if (!Auth.CanReviewSightings || Auth.Organization is not { } organization) return 0;

        var result = await SightingClient.CountAsync(organization.Id, SightingStatus.Pending, ct);
        return result.Match(count => count, _ => 0);
    }

    protected override void OnLanguageChanged() =>
        _locale = L.CurrentLanguage == "es" ? LocaleContext.Spanish : LocaleContext.English;
}
