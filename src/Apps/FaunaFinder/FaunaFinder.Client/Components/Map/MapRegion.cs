using EcoData.Spa.Map;

namespace FaunaFinder.Client.Components.Map;

/// <summary>
/// A jurisdiction the catalogue covers and the map can travel to.
/// </summary>
/// <remarks>
/// The catalogue is the FWS Caribbean Ecological Services Field Office listed-species list,
/// which spans Puerto Rico and the U.S. Virgin Islands. The two are ~150 km apart, so a single
/// view that fits both is mostly open water and too far out to read either — hence travelling
/// between them rather than showing one map.
/// </remarks>
/// <param name="Code">State code, matching the boundary endpoint and the seeded Locations state.</param>
/// <param name="NameKey">Localization key for the region's display name.</param>
/// <param name="SubtitleKey">Localization key for the "78 municipios" style subtitle.</param>
/// <param name="Center">Where to sit when no bounds fit is wanted.</param>
/// <param name="Bounds">Extent to fit when travelling here.</param>
public sealed record MapRegion(
    string Code,
    string NameKey,
    string SubtitleKey,
    MapCoordinate Center,
    MapBounds Bounds
)
{
    /// <summary>Boundary GeoJSON endpoint. The API is state-generic; only the code varies.</summary>
    public string BoundaryUrl => $"locations/municipalities/geojson/state/{Code}";

    /// <summary>Per-region localStorage key, so travelling does not evict the other region's cache.</summary>
    public string CacheKey => $"{Code.ToLowerInvariant()}-municipios-geojson";

    /// <summary>Matches a municipality to its region by FIPS state prefix (72 = PR, 78 = VI).</summary>
    public bool Owns(string geoJsonId) =>
        geoJsonId.StartsWith(FipsPrefix, StringComparison.Ordinal);

    private string FipsPrefix => Code == "PR" ? "72" : "78";
}

/// <summary>The regions the map can travel between.</summary>
public static class MapRegions
{
    public static readonly MapRegion PuertoRico = new(
        Code: "PR",
        NameKey: "Map_Region_PuertoRico",
        SubtitleKey: "Map_Region_PuertoRico_Subtitle",
        Center: new MapCoordinate(18.2208, -66.5901),
        // Tight to the served boundary extent; the wider historical box was mostly open water.
        Bounds: new MapBounds(new MapCoordinate(17.90, -67.30), new MapCoordinate(18.55, -65.20))
    );

    public static readonly MapRegion VirginIslands = new(
        Code: "VI",
        NameKey: "Map_Region_VirginIslands",
        SubtitleKey: "Map_Region_VirginIslands_Subtitle",
        Center: new MapCoordinate(18.04, -64.83),
        // St. Croix sits well south of St. Thomas and St. John, so the box is taller than it looks.
        // Fitted to the land-clipped 1:500k boundaries, cays included.
        Bounds: new MapBounds(new MapCoordinate(17.64, -65.13), new MapCoordinate(18.45, -64.52))
    );

    public static readonly IReadOnlyList<MapRegion> All = [PuertoRico, VirginIslands];

    public static MapRegion Default => PuertoRico;

    /// <summary>Resolves a region from a state code, falling back to the default.</summary>
    public static MapRegion FromCode(string? code) =>
        All.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? Default;

    /// <summary>Resolves the region owning a municipality's GeoJSON id.</summary>
    public static MapRegion ForGeoJsonId(string geoJsonId) =>
        All.FirstOrDefault(r => r.Owns(geoJsonId)) ?? Default;
}
