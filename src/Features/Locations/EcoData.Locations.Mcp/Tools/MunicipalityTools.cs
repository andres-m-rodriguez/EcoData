using System.ComponentModel;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.DataAccess.Interfaces;
using ModelContextProtocol.Server;

namespace EcoData.Locations.Mcp.Tools;

// Sealed rather than static: WithTools<T> takes the type as a generic argument,
// and a static type can't be one.
[McpServerToolType]
public sealed class MunicipalityTools
{
    // 78 Puerto Rico municipios plus 3 U.S. Virgin Islands: the default returns all of them.
    private const int DefaultResults = 81;

    private const int MaxResults = 100;

    [McpServerTool(Name = "search_municipalities")]
    [Description("""
        List or search the catalogue's places: Puerto Rico's municipios and the
        U.S. Virgin Islands. With no arguments this returns all of them with their
        ids and centre points, which is what the wildlife tools take as
        municipalityId. Pass search to narrow by name.
        """)]
    public static async Task<IReadOnlyList<MunicipalitySummary>> SearchMunicipalities(
        IMunicipalityRepository repository,
        CancellationToken cancellationToken,
        [Description("Free text matched against the municipio name.")]
        string? search = null,
        [Description("How many to return, 1-100. Defaults to 78, the whole island.")]
        int limit = DefaultResults
    )
    {
        var parameters = new MunicipalityParameters(
            PageSize: Math.Clamp(limit, 1, MaxResults),
            Search: search
        );

        var results = new List<MunicipalitySummary>();

        await foreach (var municipality in repository
            .GetMunicipalitiesAsync(parameters, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            results.Add(new MunicipalitySummary(
                municipality.Id,
                municipality.Name,
                (double)municipality.CentroidLatitude,
                (double)municipality.CentroidLongitude
            ));
        }

        return results;
    }

    [McpServerTool(Name = "get_municipality")]
    [Description("""
        Get one municipio by id, including which state it belongs to, its county
        FIPS code and its centre point. Ids come from search_municipalities or
        find_municipality_at_point.
        """)]
    public static async Task<MunicipalityDetail?> GetMunicipality(
        IMunicipalityRepository repository,
        CancellationToken cancellationToken,
        [Description("The municipio id.")] Guid id
    )
    {
        var municipality = await repository.GetByIdAsync(id, cancellationToken);

        return municipality is null ? null : ToDetail(municipality);
    }

    [McpServerTool(Name = "find_municipality_at_point")]
    [Description("""
        Find which municipio contains a point — the reverse of looking one up by
        name. Coordinates are decimal degrees (WGS 84). Returns nothing if the
        point falls outside every municipio boundary, which includes points at
        sea or off the island.
        """)]
    public static async Task<MunicipalityDetail?> FindMunicipalityAtPoint(
        IMunicipalityRepository repository,
        CancellationToken cancellationToken,
        [Description("Latitude in decimal degrees.")] double latitude,
        [Description("Longitude in decimal degrees.")] double longitude
    )
    {
        var municipality = await repository.GetByPointAsync(
            (decimal)latitude,
            (decimal)longitude,
            cancellationToken
        );

        return municipality is null ? null : ToDetail(municipality);
    }

    private static MunicipalityDetail ToDetail(
        Contracts.Dtos.MunicipalityDtoForDetail municipality
    ) =>
        new(
            municipality.Id,
            municipality.Name,
            municipality.StateName,
            municipality.StateCode,
            municipality.CountyFipsCode,
            (double)municipality.CentroidLatitude,
            (double)municipality.CentroidLongitude
        );
}
