namespace EcoData.Spa.Map;

/// <summary>
/// Interface for objects that can be displayed as markers on a map.
/// </summary>
public interface IMapMarker
{
    /// <summary>
    /// The geographic coordinate of the marker.
    /// </summary>
    MapCoordinate Coordinate { get; }

    /// <summary>
    /// Optional popup content for the marker.
    /// </summary>
    string? PopupContent { get; }

    /// <summary>
    /// Optional tooltip content for the marker.
    /// </summary>
    string? TooltipContent { get; }
}
