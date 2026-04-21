namespace EcoData.Common.Maps.Models;

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
    /// The GeoJSON data as a string.
    /// </summary>
    public required string Data { get; init; }

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
