using System.ComponentModel;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.DataAccess.Interfaces;
using ModelContextProtocol.Server;

namespace EcoData.Locations.Mcp.Tools;

/// <summary>
/// The places half of the connector: the 78 Puerto Rico municipios and the 3 U.S.
/// Virgin Islands, which are what the wildlife records are filed against.
///
/// <para>Sealed rather than static so the type can be a generic argument to
/// <c>WithTools</c>; every tool is a static method and nothing is
/// constructed.</para>
/// </summary>
[McpServerToolType]
public sealed class MunicipalityTools
{
    /// <summary>
    /// 78 Puerto Rico municipios plus 3 U.S. Virgin Islands, so the default returns
    /// all of them — the whole list is small enough to be worth having in one call,
    /// and a model that has it can resolve a name to an id without asking again.
    /// </summary>
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

        // Null reads as "no such municipio"; throwing would report a tool
        // failure for what is an ordinary answer.
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
        // The boundary test is done in the database against the stored polygon,
        // so this is a real point-in-polygon lookup rather than a nearest-
        // centroid guess.
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
