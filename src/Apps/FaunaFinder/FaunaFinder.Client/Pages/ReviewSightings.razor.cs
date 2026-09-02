using System.Globalization;
using EcoData.Common.Problems.Contracts;
using EcoData.Spa.Blazor;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using FaunaFinder.Client.Components.Sightings;
using FaunaFinder.Client.Layout;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Pages;

// The [Command]s and [Event] live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent; the
// C# symbol frontend can.
public partial class ReviewSightings : EcoDataComponent
{
    private const int PageSize = 20;
    private const int SearchPageSize = 10;

    // Keys held as consts so they can appear in Razor attribute forms
    // (`Title="@L[SomeKey]"`) without tripping the nested-quote rule.
    private const string EmptyTitleKey = "Sighting_Review_Empty_Title";

    private static readonly SightingStatus[] Statuses =
        [SightingStatus.Pending, SightingStatus.Approved, SightingStatus.Denied];

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    private readonly Dictionary<SightingStatus, int> _counts = [];
    private readonly Dictionary<Guid, string> _municipalityNames = [];
    private SightingStatus _status = SightingStatus.Pending;
    private SpeciesDtoForList? _species;

    // Null while the first page of the current tab and filter is loading.
    private List<SightingDto>? _rows;
    private bool _hasMore;

    private SightingDto? _target;
    private IDialogReference? _detail;

    private string EmptyDescriptionKey => "Sighting_Review_Empty_" + _status;

    private CultureInfo Culture => CultureInfo.GetCultureInfo(Locale.Code);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navbar.SetTitle(L["Sighting_Review_Title"]);
        SyncWithAuth();
    }

    protected override void OnLanguageChanged() => Navbar.SetTitle(L["Sighting_Review_Title"]);

    [Event]
    private void OnAuthChanged(MainLayout.AuthChanged _) => SyncWithAuth();

    private void SyncWithAuth()
    {
        if (!Auth.IsInitialized) return;

        if (!Auth.IsAuthenticated)
        {
            var here = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            NavigationManager.NavigateTo($"/login?ReturnUrl={Uri.EscapeDataString(here)}", replace: true);
            return;
        }

        if (!Auth.CanReviewSightings) return;

        _ = LoadCountsState.TryExecute();
        ResetRows();
    }

    private void ResetRows()
    {
        _rows = null;
        _hasMore = false;
        _ = LoadPageState.TryExecute();
    }

    private void SetStatus(SightingStatus status)
    {
        if (status == _status) return;

        _status = status;
        ResetRows();
    }

    private string? MunicipalityName(SightingDto sighting) =>
        sighting.MunicipalityId is { } id && _municipalityNames.TryGetValue(id, out var name) ? name : null;

    [Command]
    private async Task LoadCounts(CancellationToken ct)
    {
        if (Auth.Organization is not { } organization) return;

        var results = await Task.WhenAll(
            Statuses.Select(status => SightingClient.CountAsync(organization.Id, status, ct)));
        for (var i = 0; i < Statuses.Length; i++)
        {
            if (results[i].TryPickT0(out var count, out _)) _counts[Statuses[i]] = count;
        }
    }

    // Appends the next page after the last loaded row; a reset nulls the rows
    // first so the same command also serves the first page.
    [Command]
    private async Task LoadPage(CancellationToken ct)
    {
        if (Auth.Organization is not { } organization) return;

        var parameters = new SightingParameters(PageSize, _rows?.LastOrDefault()?.Id, _status, _species?.Id);
        var result = await SightingClient.GetByOrganizationAsync(organization.Id, parameters, ct);
        if (!result.TryPickT0(out var page, out var failed))
        {
            _rows ??= [];
            _hasMore = false;
            Snackbar.Add(FailureMessage(failed), Severity.Error);
            return;
        }

        var missing = page
            .Where(s => s.MunicipalityId is { } id && !_municipalityNames.ContainsKey(id))
            .Select(s => s.MunicipalityId!.Value)
            .Distinct()
            .ToList();
        if (missing.Count > 0)
        {
            var municipalities = await MunicipalityClient.GetByIdsAsync(missing, ct);
            if (municipalities.TryPickT0(out var found, out _))
            {
                foreach (var municipality in found) _municipalityNames[municipality.Id] = municipality.Name;
            }
        }

        (_rows ??= []).AddRange(page);
        _hasMore = page.Count == PageSize;
    }

    private async Task<IEnumerable<SpeciesDtoForList>> SearchSpecies(string? value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        var matches = new List<SpeciesDtoForList>();
        await foreach (var species in SpeciesClient.GetSpeciesAsync(
            new SpeciesParameters(PageSize: SearchPageSize, Search: value.Trim()),
            ct))
        {
            matches.Add(species);
        }
        return matches;
    }

    private async Task OpenDetail(SightingDto sighting)
    {
        var breakpoint = await Viewport.GetCurrentBreakpointAsync();

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            FullScreen = breakpoint is Breakpoint.Xs or Breakpoint.Sm,
        };

        // The dialog provider sits outside the layout's LocaleContext cascade,
        // so the locale travels as a parameter.
        var parameters = new DialogParameters<SightingDetailDialog>
        {
            { x => x.Sighting, sighting },
            { x => x.Locale, Locale },
            { x => x.CanEdit, false },
            { x => x.MunicipalityName, MunicipalityName(sighting) },
            { x => x.OnApprove, EventCallback.Factory.Create<SightingDto>(this, s => Review(s, approve: true)) },
            { x => x.OnDeny, EventCallback.Factory.Create<SightingDto>(this, s => Review(s, approve: false)) },
            { x => x.OnUnapprove, EventCallback.Factory.Create<SightingDto>(this, s => Unapprove(s)) },
        };

        _detail = await Dialogs.ShowAsync<SightingDetailDialog>(null, parameters, options);
    }

    // The review dialog owns the approve and deny calls and closes with Ok
    // once the server has accepted; the page only settles the row.
    private async Task Review(SightingDto sighting, bool approve)
    {
        if (Auth.Organization is not { } organization) return;

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
        };

        var parameters = new DialogParameters<SightingReviewDialog>
        {
            { x => x.Sighting, sighting },
            { x => x.OrganizationId, organization.Id },
            { x => x.Approve, approve },
            { x => x.Locale, Locale },
        };

        var reference = await Dialogs.ShowAsync<SightingReviewDialog>(null, parameters, options);
        var result = await reference.Result;
        if (result is null || result.Canceled) return;

        Settle(sighting, approve ? SightingStatus.Approved : SightingStatus.Denied);
    }

    private Task Unapprove(SightingDto sighting)
    {
        _target = sighting;
        return RevertToPendingState.TryExecute();
    }

    [Command]
    private async Task RevertToPending()
    {
        if (_target is not { } sighting || Auth.Organization is not { } organization) return;

        var result = await SightingClient.UnapproveAsync(organization.Id, sighting.Id);
        if (!result.TryPickT0(out _, out var failed))
        {
            Snackbar.Add(FailureMessage(failed), Severity.Error);
            return;
        }

        Settle(sighting, SightingStatus.Pending);
    }

    // The row leaves its tab and the counts shift locally, no refetch; the
    // layout re-reads its pending badge off the bus record.
    private void Settle(SightingDto sighting, SightingStatus newStatus)
    {
        _rows?.Remove(sighting);
        if (_counts.ContainsKey(sighting.Status)) _counts[sighting.Status]--;
        if (_counts.ContainsKey(newStatus)) _counts[newStatus]++;

        _detail?.Close();
        _detail = null;

        Snackbar.Add(L["Sighting_Review_Done_" + newStatus], Severity.Success);
        Bus.Publish<MainLayout.SightingsReviewed>();
    }

    private string FailureMessage(RequestFailed failed) => failed.StatusCode switch
    {
        0 => L["Sighting_Error_Unreachable"],
        401 => L["Sighting_Error_SignedOut"],
        403 => L["Sighting_Review_Error_Forbidden"],
        404 => L["Sighting_Review_Error_NotFound"],
        429 => L["Sighting_Error_TooMany"],
        _ => L["Sighting_Error_Generic"],
    };
}
