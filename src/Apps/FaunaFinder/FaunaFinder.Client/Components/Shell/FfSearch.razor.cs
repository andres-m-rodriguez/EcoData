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
// can't see that LocalizedComponentBase is a StatefulComponent; the C# symbol
// frontend can.
public partial class FfSearch : LocalizedComponentBase
{
    /// <summary>Below this, nothing is shown and no request is made.</summary>
    private const int MinQueryLength = 2;

    /// <summary>How many rows each group contributes to the dropdown.</summary>
    private const int ResultsPerGroup = 5;

    /// <summary>Puerto Rico is the only state FaunaFinder covers.</summary>
    private const string StateCode = "PR";

    // Keys held as consts so they can appear in Razor attribute forms
    // (`Placeholder="@L[SearchPlaceholderKey]"`) without tripping the
    // nested-quote rule.
    private const string SearchPlaceholderKey = "Shell_Search";
    private const string ClearLabelKey = "Shell_Search_Clear";

    /// <summary>Species names are stored per-locale; resolving needs the shell's context.</summary>
    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    /// <summary>What the reader has typed. The debounce lives on the text field.</summary>
    private string? _query;

    /// <summary>Whether the dropdown is showing. Escape, an outside click and a
    /// result click all put this back to false without touching the query.</summary>
    private bool _open;

    private string Query => _query?.Trim() ?? string.Empty;

    private bool HasText => !string.IsNullOrWhiteSpace(_query);

    /// <summary>Two or more non-whitespace characters is the floor for a request.</summary>
    private bool HasQuery => Query.Length >= MinQueryLength;

    private bool IsPanelOpen => _open && HasQuery;

    private IReadOnlyList<SpeciesDtoForList>? SpeciesResults => SearchSpeciesState.Result;

    private IReadOnlyList<MunicipalityDtoForList>? MunicipalityResults => SearchMunicipalitiesState.Result;

    private bool HasAnyRow =>
        SpeciesResults is { Count: > 0 } || MunicipalityResults is { Count: > 0 };

    /// <summary>
    /// A null command result is the loading sentinel; nothing else tracks it.
    /// The two fetches land independently, so the panel only says "searching"
    /// while one is still outstanding <em>and</em> there is nothing to show yet —
    /// a re-query keeps the previous rows up rather than flickering through an
    /// empty frame.
    /// </summary>
    private bool IsPending =>
        (SpeciesResults is null || MunicipalityResults is null) && !HasAnyRow;

    /// <summary>Both groups answered, neither had anything. A failed fetch lands
    /// here too — it returns an empty list rather than an error.</summary>
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

    /// <summary>Leaving the page drops the query outright — the bar goes back to
    /// its resting capsule rather than following the reader around.</summary>
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!HasText && !_open)
        {
            return;
        }

        Clear();
        InvokeAsync(StateHasChanged);
    }

    private async Task HandleQueryChanged(string? value)
    {
        _query = value;

        if (!HasQuery)
        {
            // Under the floor: nothing shown, nothing asked of the server.
            _open = false;
            return;
        }

        _open = true;

        await Task.WhenAll(
            SearchSpeciesState.TryExecute(),
            SearchMunicipalitiesState.TryExecute()
        );
    }

    // Focusing a pill that already holds a query brings its results back rather
    // than making the reader retype to reopen the panel.
    private void HandleFocusIn()
    {
        if (HasQuery)
        {
            _open = true;
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            Close();
        }
    }

    private void Close() => _open = false;

    private void Clear()
    {
        _query = null;
        _open = false;
    }

    private static string SpeciesHref(Guid id) => $"/species/{id}";

    private static string MunicipalityHref(Guid id) => $"/municipalities/{id}";

    // Query-driven, so neither command runs on load. The list streams carry no
    // error channel — the transport exception is the only signal — and a search
    // that can't reach the server degrades to the same empty line a genuine
    // no-match gets.
    [Command]
    private async Task<IReadOnlyList<SpeciesDtoForList>?> SearchSpecies(CancellationToken ct)
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
    private async Task<IReadOnlyList<MunicipalityDtoForList>?> SearchMunicipalities(CancellationToken ct)
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
