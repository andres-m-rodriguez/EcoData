namespace EcoData.Common.Maps.Models;

/// <summary>
/// Represents a geographic coordinate (latitude/longitude).
/// </summary>
public readonly record struct MapCoordinate(double Latitude, double Longitude)
{
    public static MapCoordinate PuertoRicoCenter => new(18.2208, -66.5901);
}
