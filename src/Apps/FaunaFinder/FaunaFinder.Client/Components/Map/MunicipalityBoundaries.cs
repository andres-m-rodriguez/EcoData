using EcoData.Spa.Map;

namespace FaunaFinder.Client.Components.Map;

/// <summary>
/// Shared definition of the municipality-boundary GeoJSON layers.
/// The data is fetched JS-side by SpaMap and cached in localStorage, so the
/// map renders immediately and repeat visits skip the network entirely.
/// </summary>
public static class MunicipalityBoundaries
{
    private const string LayerIdPrefix = "municipalities-";

    /// <summary>Bump when the seeded boundary data changes.</summary>
    public const string CacheVersion = "v1";

    /// <summary>
    /// One layer per region. The map page shows a single region and swaps layers when
    /// travelling; the municipality picker shows every region at once.
    /// </summary>
    public static string LayerIdFor(MapRegion region) => $"{LayerIdPrefix}{region.Code}";

    /// <summary>True for any region's boundary layer, so handlers need not know which.</summary>
    public static bool IsBoundaryLayer(string layerId) =>
        layerId.StartsWith(LayerIdPrefix, StringComparison.Ordinal);

    /// <summary>Builds the boundary layer for a region. Cache keys are per-region.</summary>
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
