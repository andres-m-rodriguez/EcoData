using System.ComponentModel;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using ModelContextProtocol.Server;

namespace EcoData.Wildlife.Mcp.Tools;

/// <summary>
/// The species half of the wildlife connector.
///
/// <para>Tools take their repository through the method signature rather than a
/// constructor: the server resolves each argument from the request scope, which
/// is the same scope the HTTP endpoints get, so a tool call reads the database
/// exactly as a page request would.</para>
/// </summary>
// Sealed rather than static: WithTools<T> takes the type as a generic argument,
// and a static type can't be one. The tools themselves are all static methods,
// so nothing is ever constructed.
[McpServerToolType]
public sealed class SpeciesTools
{
    /// <summary>Bounds on how much catalogue one call can pull back.</summary>
    private const int MaxResults = 50;
    private const int DefaultResults = 20;

    /// <summary>The covered islands are small; a radius past this stops meaning "near".</summary>
    private const double MaxRadiusMeters = 50_000;

    [McpServerTool(Name = "search_species")]
    [Description("""
        Search the Caribbean listed-species catalogue covering Puerto Rico and the
        U.S. Virgin Islands. Returns a summary per match;
        call get_species for the full record. Every filter is optional — with no
        arguments this returns the first page of the whole catalogue. Use
        list_taxon_categories first if you need valid taxon codes.
        """)]
    public static async Task<IReadOnlyList<SpeciesSummary>> SearchSpecies(
        ISpeciesRepository repository,
        CancellationToken cancellationToken,
        [Description("Free text matched against common and scientific names.")]
        string? search = null,
        [Description("Taxon code from list_taxon_categories, e.g. 'bird' or 'amphib'.")]
        string? taxonCode = null,
        [Description("IUCN Red List status: LC, NT, VU, EN, CR, DD or EX.")]
        string? iucnStatus = null,
        [Description("""
            Origin relative to Puerto Rico: 'Endemic' (occurs naturally only in
            Puerto Rico and its islands), 'Native' (also occurs elsewhere, the
            U.S. Virgin Islands included),
            'Native', 'Introduced', or 'Unknown' for species with no assessment
            on record. Omit for any.
            """)]
        string? endemicStatus = null,
        [Description("True for animals only, false for plants only, omit for both.")]
        bool? faunaOnly = null,
        [Description("How many to return, 1-50. Defaults to 20.")]
        int limit = DefaultResults
    )
    {
        var parameters = new SpeciesParameters(
            PageSize: Math.Clamp(limit, 1, MaxResults),
            Search: search,
            IsFauna: faunaOnly,
            EndemicStatuses: ParseEndemicStatus(endemicStatus),
            IucnStatuses: ParseIucnStatus(iucnStatus),
            TaxonCodes: string.IsNullOrWhiteSpace(taxonCode) ? null : [taxonCode]
        );

        var results = new List<SpeciesSummary>();

        await foreach (var species in repository
            .GetSpeciesAsync(parameters, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            results.Add(ToSummary(species));
        }

        return results;
    }

    [McpServerTool(Name = "get_species")]
    [Description("""
        Get the full record for one species by id, including habitat,
        taxonomic categories and how many municipios it has been recorded in.
        Ids come from search_species or find_species_near.
        """)]
    public static async Task<SpeciesDetail?> GetSpecies(
        ISpeciesRepository repository,
        CancellationToken cancellationToken,
        [Description("The species id.")] Guid id
    )
    {
        var species = await repository.GetByIdAsync(id, cancellationToken);
        if (species is null)
        {
            // Null reads as "no such species" on the wire; throwing would report
            // a tool failure for what is an ordinary answer.
            return null;
        }

        var categories = species.Categories
            .Select(category => WildlifeMcpMapping.ResolveName(category.Name, category.Code))
            .ToList();

        return new SpeciesDetail(
            species.Id,
            WildlifeMcpMapping.ResolveName(species.CommonName, species.ScientificName),
            species.ScientificName,
            WildlifeMcpMapping.Kind(species.IsFauna),
            species.IucnStatus?.ToString(),
            species.EndemicStatus.ToString(),
            species.Habitat,
            categories,
            species.MunicipalityIds.Count,
            species.LastObservedAtUtc,
            species.ImageSourceUrl
        );
    }

    [McpServerTool(Name = "find_species_near")]
    [Description("""
        Find species recorded within a radius of a point, nearest first.
        Coordinates are decimal degrees (WGS 84). Puerto Rico spans roughly
        latitude 17.9 to 18.5 and longitude -67.3 to -65.2; the U.S. Virgin
        Islands latitude 17.6 to 18.5 and longitude -65.2 to -64.5.
        """)]
    public static async Task<IReadOnlyList<NearbySpecies>> FindSpeciesNear(
        ISpeciesRepository repository,
        CancellationToken cancellationToken,
        [Description("Latitude in decimal degrees.")] double latitude,
        [Description("Longitude in decimal degrees.")] double longitude,
        [Description("Search radius in metres, up to 50000. Defaults to 5000.")]
        double radiusMeters = 5000
    )
    {
        var nearby = await repository.GetNearbyAsync(
            latitude,
            longitude,
            Math.Clamp(radiusMeters, 1, MaxRadiusMeters),
            cancellationToken
        );

        return nearby
            .Select(species => new NearbySpecies(
                species.Id,
                WildlifeMcpMapping.ResolveName(species.CommonName, species.ScientificName),
                species.ScientificName,
                WildlifeMcpMapping.Kind(species.IsFauna),
                species.DistanceMeters,
                species.LocationDescription
            ))
            .ToList();
    }

    [McpServerTool(Name = "list_species_in_municipality")]
    [Description("""
        List the species recorded in one municipio. Municipio ids come from
        search_municipalities or find_municipality_at_point. This is filed
        against the municipio a record belongs to, so unlike find_species_near
        it does not depend on a radius.
        """)]
    public static async Task<IReadOnlyList<SpeciesSummary>> ListSpeciesInMunicipality(
        ISpeciesRepository repository,
        CancellationToken cancellationToken,
        [Description("The municipio id.")] Guid municipalityId,
        [Description("How many to return, 1-50. Defaults to 20.")]
        int limit = DefaultResults
    )
    {
        var parameters = new SpeciesParameters(
            PageSize: Math.Clamp(limit, 1, MaxResults),
            MunicipalityId: municipalityId
        );

        var results = new List<SpeciesSummary>();

        await foreach (var species in repository
            .GetSpeciesAsync(parameters, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            results.Add(ToSummary(species));
        }

        return results;
    }

    [McpServerTool(Name = "get_species_richness_by_municipality")]
    [Description("""
        How many species are recorded in each municipio, for comparing one place
        against another. Returns municipio ids and counts only — pair it with
        search_municipalities, which returns every id with its name, to put
        names to them.
        """)]
    public static async Task<IReadOnlyList<MunicipalityRichness>> GetSpeciesRichnessByMunicipality(
        ISpeciesRepository repository,
        CancellationToken cancellationToken
    )
    {
        var counts = await repository.GetCountsByMunicipalityAsync(cancellationToken);

        return counts
            .Select(count => new MunicipalityRichness(count.MunicipalityId, count.Count))
            .ToList();
    }

    [McpServerTool(Name = "list_taxon_categories")]
    [Description("""
        List the taxonomic groups species are catalogued under. The codes
        returned here are what search_species accepts as taxonCode.
        """)]
    public static async Task<IReadOnlyList<TaxonCategory>> ListTaxonCategories(
        ISpeciesCategoryRepository repository,
        CancellationToken cancellationToken
    )
    {
        var categories = await repository.GetListAsync(cancellationToken);

        return categories
            .Select(category => new TaxonCategory(
                category.Code,
                WildlifeMcpMapping.ResolveName(category.Name, category.Code)
            ))
            .ToList();
    }

    [McpServerTool(Name = "get_catalogue_stats")]
    [Description("""
        Totals for the whole catalogue: species recorded, how many are endemic,
        how many are threatened (IUCN VU through CR), and municipio coverage.
        """)]
    public static async Task<CatalogueStats> GetCatalogueStats(
        ISpeciesRepository repository,
        CancellationToken cancellationToken
    )
    {
        var stats = await repository.GetStatsAsync(cancellationToken);

        return new CatalogueStats(
            stats.TotalSpecies,
            stats.EndemicCount,
            stats.ThreatenedCount,
            stats.MunicipalitiesCovered,
            stats.TotalMunicipalities
        );
    }

    private static SpeciesSummary ToSummary(Contracts.Dtos.SpeciesDtoForList species) =>
        new(
            species.Id,
            WildlifeMcpMapping.ResolveName(species.CommonName, species.ScientificName),
            species.ScientificName,
            WildlifeMcpMapping.Kind(species.IsFauna),
            species.IucnStatus?.ToString(),
            species.EndemicStatus.ToString(),
            species.TaxonCode,
            species.MunicipalityCount,
            species.LastObservedAtUtc
        );

    /// <summary>
    /// A status the model spelled itself, so an unparseable one is treated as no
    /// filter rather than an error — a search that quietly ignores a bad code
    /// still answers, and the codes are named in the tool description.
    /// </summary>
    private static IucnStatus[]? ParseIucnStatus(string? status) =>
        Enum.TryParse<IucnStatus>(status, ignoreCase: true, out var parsed)
            ? [parsed]
            : null;

    /// <inheritdoc cref="ParseIucnStatus" />
    private static EndemicStatus[]? ParseEndemicStatus(string? status) =>
        Enum.TryParse<EndemicStatus>(status, ignoreCase: true, out var parsed)
            ? [parsed]
            : null;
}
