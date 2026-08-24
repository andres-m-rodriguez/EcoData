using EcoData.Spa.Map;

namespace FaunaFinder.Client.Components.Map;

public sealed record MapRegion(
    string Code,
    string NameKey,
    string SubtitleKey,
    MapCoordinate Center,
    MapBounds Bounds
)
{
    public string BoundaryUrl => $"locations/municipalities/geojson/state/{Code}";

    public string CacheKey => $"{Code.ToLowerInvariant()}-municipios-geojson";

    // FIPS state prefix: 72 = PR, 78 = VI.
    public bool Owns(string geoJsonId) =>
        geoJsonId.StartsWith(FipsPrefix, StringComparison.Ordinal);

    private string FipsPrefix => Code == "PR" ? "72" : "78";
}

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

    public static MapRegion FromCode(string? code) =>
        All.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? Default;

    public static MapRegion ForGeoJsonId(string geoJsonId) =>
        All.FirstOrDefault(r => r.Owns(geoJsonId)) ?? Default;
}
