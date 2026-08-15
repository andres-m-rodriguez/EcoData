namespace EcoData.Spa.Map;

/// <summary>
/// Represents GeoJSON data that can be rendered on the map.
/// </summary>
public class MapGeoJson
{
    /// <summary>
    /// Unique identifier for this GeoJSON layer.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The GeoJSON data as a string. Optional when <see cref="Url"/> is set.
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// URL to fetch the GeoJSON from. The fetch happens in JS after the map has
    /// initialized, so the map renders immediately and the payload never crosses
    /// the .NET/JS interop boundary. Ignored when <see cref="Data"/> is set.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// localStorage key for caching the fetched GeoJSON. Only used with
    /// <see cref="Url"/>; when null the data is re-fetched on every visit.
    /// </summary>
    public string? CacheKey { get; init; }

    /// <summary>
    /// Cache version stored alongside <see cref="CacheKey"/>. Bump it to
    /// invalidate previously cached data.
    /// </summary>
    public string CacheVersion { get; init; } = "v1";

    /// <summary>
    /// Default fill color for features.
    /// </summary>
    public string FillColor { get; init; } = "#3388ff";

    /// <summary>
    /// Default fill opacity (0-1).
    /// </summary>
    public double FillOpacity { get; init; } = 0.2;

    /// <summary>
    /// Default stroke color for features.
    /// </summary>
    public string StrokeColor { get; init; } = "#3388ff";

    /// <summary>
    /// Default stroke width in pixels.
    /// </summary>
    public int StrokeWidth { get; init; } = 2;
}
