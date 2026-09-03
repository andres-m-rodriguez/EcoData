using EcoData.Spa.Blazor;
using System.Net.Http;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Command]s live in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name and
// can't see that EcoDataComponent is a StatefulComponent; the C# symbol
// frontend can.
public partial class FfSearch : EcoDataComponent
{
    private const int MinQueryLength = 2;

    private const int ResultsPerGroup = 5;

    private const string StateCode = "PR";

    // Keys held as consts so they can appear in Razor attribute forms
    // (`Placeholder="@L[SearchPlaceholderKey]"`) without tripping the
    // nested-quote rule.
    private const string SearchPlaceholderKey = "Shell_Search";
    private const string ClearLabelKey = "Shell_Search_Clear";

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    private string? _query;

    private bool _open;

    private string Query => _query?.Trim() ?? string.Empty;

    private bool HasText => !string.IsNullOrWhiteSpace(_query);

    private bool HasQuery => Query.Length >= MinQueryLength;

    private bool IsPanelOpen => _open && HasQuery;

    private IReadOnlyList<SpeciesDtoForList>? SpeciesResults => SearchSpeciesState.Result;

    private IReadOnlyList<MunicipalityDtoForList>? MunicipalityResults => SearchMunicipalitiesState.Result;

    private bool HasAnyRow =>
        SpeciesResults is { Count: > 0 } || MunicipalityResults is { Count: > 0 };

    private bool IsPending =>
        (SpeciesResults is null || MunicipalityResults is null) && !HasAnyRow;

    private bool ShowEmpty =>
        SpeciesResults is not null && MunicipalityResults is not null && !HasAnyRow;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navigation.LocationChanged += HandleLocationChanged;
    }

    public override void Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        base.Dispose();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!HasText && !_open)
            return;

        Clear();
        InvokeAsync(() => StateHasChanged());
    }

    private async Task HandleQueryChanged(string? value)
    {
        _query = value;

        if (!HasQuery)
        {
            _open = false;
            return;
        }

        _open = true;
        var species = SearchSpeciesState.TryExecute();
        var municipalities = SearchMunicipalitiesState.TryExecute();
        await Task.WhenAll(species, municipalities);
    }

    private void HandleFocusIn()
    {
        if (HasQuery)
            _open = true;
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
            Close();
    }

    private void Close() => _open = false;

    private void Clear()
    {
        _query = null;
        _open = false;
    }

    private static string SpeciesHref(Guid id) => $"/species/{id}";

    private static string MunicipalityHref(Guid id) => $"/municipalities/{id}";

    private static string SpeciesImageSrc(Guid id) => $"/wildlife/species/{id}/image";

    // Query-driven, so neither command runs on load. The list streams carry no
    // error channel — the transport exception is the only signal — and a search
    // that can't reach the server degrades to the same empty line a genuine
    // no-match gets.
    [Command]
    private async Task<IReadOnlyList<SpeciesDtoForList>> SearchSpecies(CancellationToken ct)
    {
        var collected = new List<SpeciesDtoForList>();

        try
        {
            await foreach (var species in SpeciesClient.GetSpeciesAsync(
                               new SpeciesParameters(PageSize: ResultsPerGroup, Search: Query),
                               ct))
            {
                collected.Add(species);
            }
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return collected;
    }

    [Command]
    private async Task<IReadOnlyList<MunicipalityDtoForList>> SearchMunicipalities(CancellationToken ct)
    {
        var collected = new List<MunicipalityDtoForList>();

        try
        {
            await foreach (var municipality in MunicipalityClient.GetMunicipalitiesAsync(
                               new MunicipalityParameters(
                                   PageSize: ResultsPerGroup,
                                   StateCode: StateCode,
                                   Search: Query),
                               ct))
            {
                collected.Add(municipality);
            }
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return collected;
    }
}
