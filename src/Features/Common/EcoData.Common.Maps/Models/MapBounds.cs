namespace EcoData.Common.Maps.Models;

/// <summary>
/// Represents geographic bounds (bounding box).
/// </summary>
public readonly record struct MapBounds(MapCoordinate SouthWest, MapCoordinate NorthEast)
{
    public static MapBounds PuertoRico => new(
        new MapCoordinate(17.88, -67.95),
        new MapCoordinate(18.52, -65.22)
    );
}
