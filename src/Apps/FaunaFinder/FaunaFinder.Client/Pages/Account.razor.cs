using EcoData.Spa.Blazor;
using EcoData.Wildlife.Application.Client;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using FaunaFinder.Client.Layout;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using Tempest;

namespace FaunaFinder.Client.Pages;

// Tempest attributes live in the code-behind: the razor frontend can't see
// that EcoDataComponent is a StatefulComponent (see EcoDataComponent).
public partial class Account : EcoDataComponent
{
    private const int RecentCount = 5;

    // Keys held as consts so they can appear in Razor attribute forms
    // (`Title="@L[SomeKey]"`) without tripping the nested-quote rule.
    private const string EmptyTitleKey = "Sighting_Mine_Empty_Title";
    private const string EmptyDescriptionKey = "Sighting_Mine_Empty_Description";

    [Inject]
    private ISightingHttpClient SightingClient { get; set; } = default!;

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navbar.SetTitle(L["Account_PageTitle"]);
        Refresh();
    }

    protected override void OnLanguageChanged() => Navbar.SetTitle(L["Account_PageTitle"]);

    [Event]
    private void OnAuthChanged(MainLayout.AuthChanged _) => Refresh();

    [Event]
    private void OnSightingsReviewed(MainLayout.SightingsReviewed _) => LoadPendingCountState.TryExecute();

    private void Refresh()
    {
        LoadPendingCountState.TryExecute();
        LoadRecentState.TryExecute();
    }

    [Command]
    private async Task<int> LoadPendingCount(CancellationToken ct)
    {
        if (!Auth.CanReviewSightings || Auth.Organization is not { } organization) return 0;

        var result = await SightingClient.CountAsync(organization.Id, SightingStatus.Pending, ct);
        return result.Match(count => count, _ => 0);
    }

    [Command]
    private async Task<IReadOnlyList<SightingDto>?> LoadRecent(CancellationToken ct)
    {
        if (!Auth.IsAuthenticated) return null;

        var recent = new List<SightingDto>(RecentCount);
        await foreach (var sighting in SightingClient.GetMineAsync(new SightingParameters(PageSize: RecentCount), ct))
        {
            recent.Add(sighting);
        }

        return recent;
    }

    [Command]
    private async Task SignOut()
    {
        await Auth.LogoutAsync();
        Navigation.NavigateTo("/");
    }
}
