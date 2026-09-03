using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace EcoData.Locations.Helpers;

public static class GeometryHelpers
{
    public static GeometryFactory GeometryFactory { get; } =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static Point? CreatePoint(decimal latitude, decimal longitude)
    {
        if (latitude == 0 && longitude == 0)
            return null;

        return GeometryFactory.CreatePoint(new Coordinate((double)longitude, (double)latitude));
    }

    public static Polygon CreatePolygon(double[][] coordinates)
    {
        var points = coordinates.Select(c => new Coordinate(c[0], c[1])).ToArray();
        var ring = GeometryFactory.CreateLinearRing(points);
        return GeometryFactory.CreatePolygon(ring);
    }
}
