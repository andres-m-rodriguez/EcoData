namespace EcoData.Spa.Map;

/// <summary>
/// Represents geographic bounds (bounding box).
/// </summary>
public readonly record struct MapBounds(MapCoordinate SouthWest, MapCoordinate NorthEast);
