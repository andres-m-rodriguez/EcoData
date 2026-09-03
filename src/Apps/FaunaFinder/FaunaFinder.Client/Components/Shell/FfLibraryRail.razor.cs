using EcoData.Spa.Blazor;
using System.Globalization;
using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.FieldNotebook;
using FaunaFinder.Client.Services.Geolocation;
using Microsoft.AspNetCore.Components;
using EcoData.Ui.Interop;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Command]s live in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name and
// can't see that EcoDataComponent is a StatefulComponent; the C# symbol
// frontend can.
public partial class FfLibraryRail : EcoDataComponent
{
    private const int SkeletonRowCount = 6;

    private const double NearbyRadiusMeters = 5000;

    private const double NearbyRadiusKm = NearbyRadiusMeters / 1000;

    private const string GeolocationDenied = "denied";
    private const string GeolocationUnsupported = "unsupported";

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    [Inject]
    private IJavascriptSafeInterop JS { get; set; } = default!;

    private RailTab _tab = RailTab.Saved;

    private NearbyState _nearby = NearbyState.Idle;

    private double _originLatitude;
    private double _originLongitude;

    private bool ActiveListPending => _tab switch
    {
        RailTab.Saved => LoadSavedState.Result is null,
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

    private void HandleNotebookChanged() => _ = InvokeAsync(RefreshNotebookAsync);

    private async Task RefreshNotebookAsync()
    {
        await LoadSavedState.TryExecute();
        StateHasChanged();
    }

    private void ShowSaved() => _tab = RailTab.Saved;

    private void ShowNearby() => _tab = RailTab.Nearby;

    private string ChipClass(RailTab tab) =>
        tab == _tab ? "ff-rail-chip ff-rail-chip--active" : "ff-rail-chip";

    private string AriaPressed(RailTab tab) => tab == _tab ? "true" : "false";

    private static string SpeciesHref(Guid id) => $"/species/{id}";

    private static string SpeciesImageSrc(Guid id) => $"/wildlife/species/{id}/image";

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

    [Command]
    private async Task<IReadOnlyList<SpeciesNearbyDto>?> LoadNearby(CancellationToken ct)
    {
        var result = await SpeciesClient.GetNearbyAsync(
            _originLatitude,
            _originLongitude,
            NearbyRadiusMeters,
            ct);

        if (!result.TryPickT0(out var nearby, out _))
            return [];

        return nearby;
    }

    private enum RailTab
    {
        Saved,
        Nearby
    }

    private enum NearbyState
    {
        Idle,
        Locating,
        Denied,
        Unsupported,
        Ready
    }

    private readonly record struct GeolocationOutcome(
        double Latitude,
        double Longitude,
        string? Error
    );
}
