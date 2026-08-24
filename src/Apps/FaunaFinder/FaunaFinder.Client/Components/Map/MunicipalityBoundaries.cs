using EcoData.Spa.Map;

namespace FaunaFinder.Client.Components.Map;

public static class MunicipalityBoundaries
{
    private const string LayerIdPrefix = "municipalities-";

    // Bump when the seeded boundary data changes.
    public const string CacheVersion = "v1";

    public static string LayerIdFor(MapRegion region) => $"{LayerIdPrefix}{region.Code}";

    public static bool IsBoundaryLayer(string layerId) =>
        layerId.StartsWith(LayerIdPrefix, StringComparison.Ordinal);

    public static MapGeoJson ForRegion(
        MapRegion region,
        string fillColor = "#40916c",
        double fillOpacity = 0.3,
        string strokeColor = "#2d6a4f",
        int strokeWidth = 2
    ) =>
        new()
        {
            Id = LayerIdFor(region),
            Url = region.BoundaryUrl,
            CacheKey = region.CacheKey,
            CacheVersion = CacheVersion,
            FillColor = fillColor,
            FillOpacity = fillOpacity,
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth,
        };
}
