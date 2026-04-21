using EcoData.Common.Maps.Models;

namespace FaunaFinder.Client.Models;

public class SightingMarker : IMapMarker
{
    public required string SpeciesName { get; init; }
    public required MapCoordinate Coordinate { get; init; }
    public DateTime SightedAt { get; init; }

    public string? PopupContent => $"<strong>{SpeciesName}</strong><br/>Sighted: {SightedAt:MMM d, yyyy}";
    public string? TooltipContent => SpeciesName;
}
