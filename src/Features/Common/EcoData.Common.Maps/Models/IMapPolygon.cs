namespace EcoData.Common.Maps.Models;

/// <summary>
/// Interface for objects that can be displayed as polygons on a map.
/// </summary>
public interface IMapPolygon
{
    /// <summary>
    /// Unique identifier for the polygon.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The coordinates forming the polygon boundary.
    /// </summary>
    IReadOnlyList<MapCoordinate> Coordinates { get; }

    /// <summary>
    /// Fill color (CSS color string, e.g., "#3388ff" or "rgba(51,136,255,0.5)").
    /// </summary>
    string? FillColor { get; }

    /// <summary>
    /// Fill opacity (0-1).
    /// </summary>
    double FillOpacity { get; }

    /// <summary>
    /// Stroke/border color.
    /// </summary>
    string? StrokeColor { get; }

    /// <summary>
    /// Stroke width in pixels.
    /// </summary>
    int StrokeWidth { get; }

    /// <summary>
    /// Optional popup content for the polygon.
    /// </summary>
    string? PopupContent { get; }

    /// <summary>
    /// Optional tooltip content for the polygon.
    /// </summary>
    string? TooltipContent { get; }
}
