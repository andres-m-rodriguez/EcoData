using System.Net.Http;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Localization;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Command]s live in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name and
// can't see that LocalizedComponentBase is a StatefulComponent; the C# symbol
// frontend can.
public partial class FfLibraryRail : LocalizedComponentBase
{
    private const int MunicipioPageSize = 200;
    private const int TopMunicipioCount = 12;
    private const int SkeletonRowCount = 6;
    private const string PuertoRicoStateCode = "PR";

    /// <summary>Which list the chips are showing. Purely local — switching never refetches.</summary>
    private RailTab _tab = RailTab.Municipios;

    /// <summary>
    /// A null command result is the loading sentinel; nothing else tracks it.
    /// </summary>
    private bool ActiveListPending => _tab == RailTab.Municipios
        ? LoadMunicipiosState.Result is null
        : LoadTaxaState.Result is null;

    private void ShowMunicipios() => _tab = RailTab.Municipios;

    private void ShowTaxa() => _tab = RailTab.Taxa;

    private string ChipClass(RailTab tab) =>
        tab == _tab ? "ff-rail-chip ff-rail-chip--active" : "ff-rail-chip";

    private string AriaPressed(RailTab tab) => tab == _tab ? "true" : "false";

    private static string MunicipioHref(Guid id) => $"/municipalities/{id}";

    [Command, RunOnLoad]
    private async Task<IReadOnlyList<MunicipioEntry>> LoadMunicipios(CancellationToken ct)
    {
        // Names stream out of Locations while the tallies come from Wildlife —
        // start the count fetch first so the two run in parallel, then join.
        var countsTask = SpeciesClient.GetCountsByMunicipalityAsync(ct);

        var collected = new List<MunicipalityDtoForList>();
        try
        {
            await foreach (var municipality in MunicipalityClient.GetMunicipalitiesAsync(
                               new MunicipalityParameters(
                                   PageSize: MunicipioPageSize,
                                   StateCode: PuertoRicoStateCode),
                               ct))
            {
                collected.Add(municipality);
            }
        }
        catch (HttpRequestException)
        {
            // The list stream has no error channel — the transport exception is
            // the signal. The rail is secondary chrome, so a failure degrades to
            // the same quiet Rail_Empty line an empty result gets.
            return [];
        }

        var countsResult = await countsTask;
        if (!countsResult.TryPickT0(out var counts, out _))
        {
            return [];
        }

        var namesById = collected.ToDictionary(m => m.Id, m => m.Name);
        return counts
            .Where(c => c.Count > 0 && namesById.ContainsKey(c.MunicipalityId))
            .OrderByDescending(c => c.Count)
            .Take(TopMunicipioCount)
            .Select(c => new MunicipioEntry(c.MunicipalityId, namesById[c.MunicipalityId], c.Count))
            .ToList();
    }

    [Command, RunOnLoad]
    private async Task<IReadOnlyList<TaxonFacetDto>> LoadTaxa(CancellationToken ct)
    {
        var result = await SpeciesCategoryClient.GetCountsAsync(ct);
        if (!result.TryPickT0(out var counts, out _))
        {
            return [];
        }

        return counts;
    }

    private enum RailTab
    {
        Municipios,
        Taxa
    }

    private sealed record MunicipioEntry(Guid Id, string Name, int Count);
}
