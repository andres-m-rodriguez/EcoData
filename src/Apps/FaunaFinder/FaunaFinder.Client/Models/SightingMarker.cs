using EcoData.Spa.Map;

namespace FaunaFinder.Client.Models;

// The single pin on a sighting map: the report form moves it around, the
// detail dialog shows where a report landed.
public sealed record SightingMarker(
    MapCoordinate Coordinate,
    string? PopupContent = null,
    string? TooltipContent = null) : IMapMarker;
