using System.Net;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Spa.Blazor;
using EcoData.Spa.Map;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.Contracts.Validators;
using FaunaFinder.Client.Components.Map;
using FaunaFinder.Client.Layout;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Pages;

// The [Command]s and [Event] live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent; the
// C# symbol frontend can.
public partial class ReportSighting : EcoDataComponent
{
    private const int NoteMaxLength = 2000;
    private const int RegionZoom = 9;
    private const int PointZoom = 14;
    private const int SearchPageSize = 10;
    private const int MaxImages = 5;
    private const long MaxImageBytes = 10 * 1024 * 1024;

    [SupplyParameterFromQuery(Name = "speciesId")]
    public Guid? SpeciesId { get; set; }

    [SupplyParameterFromQuery(Name = "lat")]
    public double? Lat { get; set; }

    [SupplyParameterFromQuery(Name = "lng")]
    public double? Lng { get; set; }

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    private readonly MapController<SightingMarker> _mapController = new();

    private SpeciesDtoForList? _species;
    private MapCoordinate? _point;
    private MunicipalityDtoForDetail? _municipality;
    private bool _outsideMunicipality;
    private DateTime? _observedDate = DateTime.Today;
    private TimeSpan? _observedTime = DateTime.Now.TimeOfDay;
    private int? _count;
    private string _note = string.Empty;

    private string? _speciesError;
    private string? _locationError;
    private string? _observedError;
    private string? _countError;
    private string? _noteError;

    private readonly List<IBrowserFile> _files = [];

    private bool _prefilled;

    private string MunicipalityLabel =>
        ResolveMunicipalityState.IsLoading ? L["Sighting_Municipality_Resolving"]
        : _municipality is { } municipality ? municipality.Name
        : _outsideMunicipality ? L["Sighting_Municipality_Outside"]
        : L["Sighting_Municipality_Unknown"];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navbar.SetTitle(L["Sighting_Report_Title"]);
        _mapController.OnMapClicked += OnMapClicked;
        _mapController.SetView(MapRegions.Default.Center, RegionZoom);
        RedirectIfSignedOut();
    }

    protected override void OnParametersSet()
    {
        if (_prefilled) return;
        _prefilled = true;

        if (Lat is { } lat && Lng is { } lng)
        {
            var point = new MapCoordinate(lat, lng);
            MovePoint(point);
            _mapController.SetView(point, PointZoom);
        }

        if (SpeciesId is not null)
            _ = LoadSpeciesState.TryExecute();
    }

    protected override void OnLanguageChanged() => Navbar.SetTitle(L["Sighting_Report_Title"]);

    [Event]
    private void OnAuthChanged(MainLayout.AuthChanged _) => RedirectIfSignedOut();

    private void RedirectIfSignedOut()
    {
        if (!Auth.IsInitialized || Auth.IsAuthenticated) return;

        var here = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/login?ReturnUrl={Uri.EscapeDataString(here)}", replace: true);
    }

    private void OnMapClicked(MapCoordinate point) =>
        _ = InvokeAsync(() =>
        {
            MovePoint(point);
            StateHasChanged();
        });

    private void MovePoint(MapCoordinate point)
    {
        _point = point;
        _locationError = null;
        _mapController.SetMarkers([new SightingMarker(point)]);
        _ = ResolveMunicipalityState.TryExecute();
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

    // The autocomplete holds list rows and the catalogue has no list-shaped
    // lookup by id, so the pre-selected species is found by searching its own
    // scientific name and keeping the row with the matching id.
    [Command]
    private async Task LoadSpecies(CancellationToken ct)
    {
        var detail = await SpeciesClient.GetByIdAsync(SpeciesId!.Value, ct);
        if (!detail.TryPickT0(out var species, out _)) return;

        await foreach (var candidate in SpeciesClient.GetSpeciesAsync(
            new SpeciesParameters(PageSize: SearchPageSize, Search: species.ScientificName),
            ct))
        {
            if (candidate.Id != species.Id) continue;
            _species = candidate;
            _speciesError = null;
            return;
        }
    }

    [Command]
    private async Task ResolveMunicipality(CancellationToken ct)
    {
        if (_point is not { } point) return;

        _municipality = null;
        _outsideMunicipality = false;
        var result = await MunicipalityClient.GetByPointAsync(point.Latitude, point.Longitude, ct);
        if (!result.TryPickT0(out var municipality, out var error))
        {
            _outsideMunicipality = error.StatusCode == 404;
            return;
        }
        _municipality = municipality;
    }

    [Command]
    private async Task UseMyLocation()
    {
        var position = await _mapController.GetCurrentPositionAsync();
        if (position is null) return;

        if (!position.Success)
        {
            var key = position.Error switch
            {
                "denied" => "Map_Geolocation_Denied",
                "unsupported" => "Map_Geolocation_Unsupported",
                _ => "Map_Geolocation_Unavailable",
            };
            Snackbar.Add(L[key], Severity.Warning);
            return;
        }

        var point = new MapCoordinate(position.Latitude, position.Longitude);
        MovePoint(point);
        _mapController.SetView(point, PointZoom);
    }

    [Command]
    private async Task Submit()
    {
        _speciesError = _species is null ? L["Sighting_Species_Required"] : null;
        _locationError = _point is null ? L["Sighting_Location_Required"] : null;
        _observedError = _observedDate is null || _observedTime is null ? L["Sighting_ObservedAt_Required"] : null;
        _countError = null;
        _noteError = null;
        if (_speciesError is not null || _locationError is not null || _observedError is not null) return;

        var observedLocal = _observedDate!.Value.Date.Add(_observedTime!.Value);
        var dto = new SightingDtoForCreate(
            _species!.Id,
            _point!.Value.Latitude,
            _point.Value.Longitude,
            _municipality?.Id,
            new DateTimeOffset(observedLocal).ToUniversalTime(),
            _count,
            string.IsNullOrWhiteSpace(_note) ? null : _note.Trim());

        var validation = new SightingDtoForCreateValidator().Validate(dto);
        if (!validation.IsValid)
        {
            var fieldErrors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            ApplyFieldErrors(fieldErrors);
            return;
        }

        if (Auth.Organization is not { } organization) return;

        var result = await SightingClient.ReportAsync(organization.Id, dto);
        if (result.TryPickT1(out var validationFailed, out var remainder))
        {
            ApplyFieldErrors(validationFailed.Errors);
            return;
        }

        if (remainder.TryPickT1(out var requestFailed, out var sighting))
        {
            var message = requestFailed.StatusCode switch
            {
                HttpStatusCode.Unauthorized => L["Sighting_Error_SignedOut"],
                HttpStatusCode.Forbidden => L["Sighting_Error_Forbidden"],
                HttpStatusCode.NotFound => L["Sighting_Error_NotFound"],
                HttpStatusCode.TooManyRequests => L["Sighting_Error_TooMany"],
                _ when requestFailed.IsTransportFailure => L["Sighting_Error_Unreachable"],
                _ => L["Sighting_Error_Generic"],
            };
            Snackbar.Add(message, Severity.Error);
            return;
        }

        // The sighting is saved from here on; a photo that fails to upload is
        // reported and skipped, never a reason to lose the report.
        foreach (var file in _files)
        {
            await using var content = file.OpenReadStream(MaxImageBytes);
            var upload = await SightingClient.UploadImageAsync(sighting.Id, content, file.Name, file.ContentType);
            if (!upload.IsT0)
                Snackbar.Add(L["Sighting_Image_UploadFailed", file.Name], Severity.Warning);
        }

        Snackbar.Add(L["Sighting_Report_Success"], Severity.Success);
        NavigationManager.NavigateTo("/sightings/mine");
    }

    private void OnFilesPicked(IReadOnlyList<IBrowserFile>? picked)
    {
        if (picked is null) return;

        foreach (var file in picked)
        {
            if (_files.Count >= MaxImages)
            {
                Snackbar.Add(L["Sighting_Image_TooMany", MaxImages], Severity.Warning);
                return;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                Snackbar.Add(L["Sighting_Image_NotImage", file.Name], Severity.Warning);
                continue;
            }

            if (file.Size > MaxImageBytes)
            {
                Snackbar.Add(L["Sighting_Image_TooLarge", file.Name], Severity.Warning);
                continue;
            }

            _files.Add(file);
        }
    }

    // Keys arrive as PascalCase from the inline validator and in whatever
    // casing the server's problem details use, so they are matched loosely.
    private void ApplyFieldErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (property, messages) in errors)
        {
            var text = string.Join(" ", messages);
            if (property.Contains("Species", StringComparison.OrdinalIgnoreCase)) _speciesError = text;
            else if (property.Contains("Latitude", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Longitude", StringComparison.OrdinalIgnoreCase)
                || property.Contains("Municipality", StringComparison.OrdinalIgnoreCase)) _locationError = text;
            else if (property.Contains("Observed", StringComparison.OrdinalIgnoreCase)) _observedError = text;
            else if (property.Contains("Count", StringComparison.OrdinalIgnoreCase)) _countError = text;
            else if (property.Contains("Note", StringComparison.OrdinalIgnoreCase)) _noteError = text;
            else Snackbar.Add(text, Severity.Error);
        }
    }

    public override void Dispose()
    {
        _mapController.OnMapClicked -= OnMapClicked;
        base.Dispose();
    }
}
