using EcoData.Spa.Map;
using EcoData.Wildlife.Contracts.Parameters;
using FaunaFinder.Client.Services.Shapes;

namespace FaunaFinder.Client.Services.MapSearch;

// Holds the search the map had open when the reader left for a species, so
// coming back reopens it instead of an empty map. The map takes it once.
public sealed class MapSearchReturn
{
    public MapSearchSnapshot? Pending { get; set; }
}

public sealed record MapSearchSnapshot(
    AreaSearchMode Mode,
    MapCoordinate? Origin,
    double RadiusMeters,
    IReadOnlyList<PolygonCoordinate>? Polygon,
    ShapeArea? Shape
);

public enum AreaSearchMode
{
    None,
    Point,
    NearMe,
    Coordinates,
    DrawnArea,
    ShapeFile,
}
