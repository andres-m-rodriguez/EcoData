using System.Globalization;
using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.FieldNotebook;
using FaunaFinder.Client.Services.Geolocation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Command]s live in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name and
// can't see that LocalizedComponentBase is a StatefulComponent; the C# symbol
// frontend can.
public partial class FfLibraryRail : LocalizedComponentBase
{
    private const int SkeletonRowCount = 6;

    /// <summary>How far "near me" reaches. Kept in metres for the API and in
    /// kilometres for the copy, which reads in kilometres on both languages.</summary>
    private const double NearbyRadiusMeters = 5000;

    private const double NearbyRadiusKm = NearbyRadiusMeters / 1000;

    /// <summary>The error codes a browser geolocation lookup can report.</summary>
    private const string GeolocationDenied = "denied";
    private const string GeolocationUnsupported = "unsupported";

    /// <summary>Species names are stored per-locale; resolving needs the shell's context.</summary>
    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Which list the chips are showing. Purely local — switching never refetches.</summary>
    private RailTab _tab = RailTab.Saved;

    /// <summary>Where the nearby lookup stands. Starts cold and only ever moves on a click.</summary>
    private NearbyState _nearby = NearbyState.Idle;

    private double _originLatitude;
    private double _originLongitude;

    /// <summary>
    /// A null command result is the loading sentinel; nothing else tracks it.
    /// Nearby is only ever pending once a position has actually been resolved —
    /// before that the tab is showing its prompt or an explanation instead.
    /// </summary>
    private bool ActiveListPending => _tab switch
    {
        RailTab.Saved => LoadSavedState.Result is null,
        RailTab.Recent => LoadRecentState.Result is null,
        _ => _nearby == NearbyState.Ready && LoadNearbyState.Result is null,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Notebook.Changed += HandleNotebookChanged;
    }

    public override void Dispose()
    {
        Notebook.Changed -= HandleNotebookChanged;
        base.Dispose();
    }

    // Changed is raised from whatever page starred or opened something, off this
    // component's synchronization context, so hop back before touching state.
    private void HandleNotebookChanged() => _ = InvokeAsync(RefreshNotebookAsync);

    private async Task RefreshNotebookAsync()
    {
        await LoadSavedState.TryExecute();
        await LoadRecentState.TryExecute();
        StateHasChanged();
    }

    private void ShowSaved() => _tab = RailTab.Saved;

    private void ShowRecent() => _tab = RailTab.Recent;

    private void ShowNearby() => _tab = RailTab.Nearby;

    private string ChipClass(RailTab tab) =>
        tab == _tab ? "ff-rail-chip ff-rail-chip--active" : "ff-rail-chip";

    private string AriaPressed(RailTab tab) => tab == _tab ? "true" : "false";

    private static string SpeciesHref(Guid id) => $"/species/{id}";

    private static string EntryHref(NotebookEntry entry) => entry.Kind switch
    {
        NotebookEntryKind.Municipality => $"/municipalities/{entry.Id}",
        _ => SpeciesHref(entry.Id),
    };

    private static string ArtClass(NotebookEntryKind kind) =>
        kind == NotebookEntryKind.Municipality
            ? "ff-rail-art ff-rail-art--place"
            : "ff-rail-art ff-rail-art--species";

    private static string ArtIcon(NotebookEntryKind kind) =>
        kind == NotebookEntryKind.Municipality
            ? "fa-solid fa-mountain-sun"
            : "fa-solid fa-paw";

    // The status pill palette is global (fauna-tokens.css), keyed by IUCN code.
    private static string StatusPillClass(string status) => $"ff-status-pill status-{status}";

    /// <summary>
    /// Coarse "how long ago" for a visited entry — the notebook only ever shows
    /// the newest twenty, so nothing beyond days needs a phrasing of its own.
    /// </summary>
    private string RelativeTime(DateTimeOffset recordedAtUtc)
    {
        var elapsed = DateTimeOffset.UtcNow - recordedAtUtc;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return L["Time_JustNow"];
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return L["Time_MinutesAgo", (int)elapsed.TotalMinutes];
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return L["Time_HoursAgo", (int)elapsed.TotalHours];
        }

        return L["Time_DaysAgo", (int)elapsed.TotalDays];
    }

    private string FormatDistance(double meters) =>
        meters < 1000
            ? L["Rail_Distance_M", meters.ToString("N0", CultureInfo.CurrentCulture)]
            : L["Rail_Distance_Km", (meters / 1000).ToString("N1", CultureInfo.CurrentCulture)];

    private async Task RequestNearbyAsync()
    {
        _nearby = NearbyState.Locating;

        var position = await ResolvePositionAsync();
        if (position.Error is { } error)
        {
            _nearby = error == GeolocationDenied ? NearbyState.Denied : NearbyState.Unsupported;
            return;
        }

        _originLatitude = position.Latitude;
        _originLongitude = position.Longitude;
        _nearby = NearbyState.Ready;

        await LoadNearbyState.TryExecute();
    }

    /// <summary>
    /// Asks the browser where the reader is.
    ///
    /// <para>The only geolocation call in the codebase today is welded to
    /// <c>NuiMap</c>'s ES module, and the rail must not render a hidden map to
    /// borrow it. Until FaunaFinder's own module exports the lookup this reports
    /// "unsupported", so the tab says so plainly instead of pretending to
    /// search. Swapping in the interop call is the only change the path needs —
    /// everything downstream of it is already wired.</para>
    /// </summary>
    // The map gets its location through IMapController, which only exists
    // inside NuiMap — borrowing it here would mean rendering a hidden map.
    // BrowserGeolocation is the same capability without the component, and
    // it never throws: a failed import reports as Unavailable.
    private async Task<GeolocationOutcome> ResolvePositionAsync()
    {
        var position = await BrowserGeolocation.GetPositionAsync(JS);

        return position.Status switch
        {
            GeoStatus.Ok => new GeolocationOutcome(position.Latitude, position.Longitude, null),
            GeoStatus.Denied => new GeolocationOutcome(0, 0, GeolocationDenied),
            _ => new GeolocationOutcome(0, 0, GeolocationUnsupported),
        };
    }

    [Command, RunOnLoad]
    private async Task<IReadOnlyList<NotebookEntry>?> LoadSaved(CancellationToken ct) =>
        await Notebook.GetSavedAsync(ct);

    [Command, RunOnLoad]
    private async Task<IReadOnlyList<NotebookEntry>?> LoadRecent(CancellationToken ct) =>
        await Notebook.GetRecentAsync(ct);

    // User-triggered: runs only once RequestNearbyAsync has an origin to search from.
    [Command]
    private async Task<IReadOnlyList<SpeciesNearbyDto>?> LoadNearby(CancellationToken ct)
    {
        var result = await SpeciesClient.GetNearbyAsync(
            _originLatitude,
            _originLongitude,
            NearbyRadiusMeters,
            ct);

        if (!result.TryPickT0(out var nearby, out _))
        {
            // The rail is secondary chrome, so a failure degrades to the same
            // quiet empty line an empty radius gets.
            return [];
        }

        return nearby;
    }

    private enum RailTab
    {
        Saved,
        Recent,
        Nearby
    }

    private enum NearbyState
    {
        /// <summary>Nothing asked of the browser yet — the tab shows its prompt.</summary>
        Idle,
        Locating,
        Denied,
        Unsupported,

        /// <summary>A position is in hand; the species list is the loading sentinel from here.</summary>
        Ready
    }

    /// <summary>
    /// What a geolocation lookup answers: a position, or one of the error codes
    /// above. Mirrors the shape <c>nuimap.js</c>'s <c>getCurrentPosition</c> returns.
    /// </summary>
    private readonly record struct GeolocationOutcome(
        double Latitude,
        double Longitude,
        string? Error
    );
}
