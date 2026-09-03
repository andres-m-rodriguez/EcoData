using EcoData.Wildlife.Contracts.Parameters;

namespace FaunaFinder.Client.Services.Shapes;

// The outer rings read from an uploaded file, each one a closed polygon the
// map can search on its own.
public sealed record ShapeArea(string Name, IReadOnlyList<IReadOnlyList<PolygonCoordinate>> Polygons);
