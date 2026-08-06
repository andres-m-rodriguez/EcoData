namespace FaunaFinder.Client.Components.Map;

/// <summary>
/// Shared definition of the Puerto Rico municipality-boundary GeoJSON layer.
/// The data is fetched JS-side by NuiMap and cached in localStorage, so the
/// map renders immediately and repeat visits skip the network entirely.
/// </summary>
public static class MunicipalityBoundaries
{
    public const string LayerId = "municipalities";

    public const string Url = "locations/municipalities/geojson/state/PR";

    public const string CacheKey = "pr-municipios-geojson";

    /// <summary>Bump when the seeded boundary data changes.</summary>
    public const string CacheVersion = "v1";
}
