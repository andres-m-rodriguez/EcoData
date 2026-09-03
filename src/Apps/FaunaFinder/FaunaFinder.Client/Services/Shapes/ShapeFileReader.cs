using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;

namespace FaunaFinder.Client.Services.Shapes;

public enum ShapeReadFailure
{
    Unreadable,
    NoPolygon,
    NotWgs84,
}

public sealed record ShapeReadFailed(ShapeReadFailure Reason);

// Reads the outer rings out of GeoJSON, KML, or an ESRI Shapefile (bare .shp or
// a .zip holding one). Everything runs in the browser; the server only ever
// sees plain coordinate lists.
public static class ShapeFileReader
{
    public const int MaxPolygons = 50;

    // Enough to keep a coastline's shape; a denser ring is thinned to this.
    public const int MaxPointsPerRing = 2000;

    private const int ShapefileMagic = 9994;
    private const int ShapefileHeaderBytes = 100;

    public static OneOf<ShapeArea, ShapeReadFailed> Read(string fileName, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // Malformed input is the expected failure at this boundary, so the
        // parsers' own exceptions are the signal for "unreadable".
        OneOf<List<IReadOnlyList<PolygonCoordinate>>, ShapeReadFailed> rings;
        try
        {
            rings = extension switch
            {
                ".geojson" or ".json" => ReadGeoJson(content),
                ".kml" => ReadKml(content),
                ".zip" => ReadZip(content),
                ".shp" => ReadShapefile(content, projection: null),
                _ => new ShapeReadFailed(ShapeReadFailure.Unreadable),
            };
        }
        catch (Exception e) when (e is JsonException or XmlException or InvalidDataException or ArgumentOutOfRangeException)
        {
            return new ShapeReadFailed(ShapeReadFailure.Unreadable);
        }

        if (!rings.TryPickT0(out var outerRings, out var failed))
            return failed;

        var polygons = outerRings
            .Select(ring => Normalize(ring))
            .Where(ring => ring.Count >= 3)
            .OrderByDescending(ring => ring.Count)
            .Take(MaxPolygons)
            .ToList();

        if (polygons.Count == 0)
            return new ShapeReadFailed(ShapeReadFailure.NoPolygon);

        var outOfRange = polygons.Any(ring =>
            ring.Any(point => Math.Abs(point.Latitude) > 90 || Math.Abs(point.Longitude) > 180));
        if (outOfRange)
            return new ShapeReadFailed(ShapeReadFailure.NotWgs84);

        var name = Path.GetFileName(fileName);
        return new ShapeArea(name, polygons);
    }

    // Drops the closing repeat of the first point and thins dense rings so a
    // request stays small and the server's per-vertex loop stays cheap.
    private static IReadOnlyList<PolygonCoordinate> Normalize(IReadOnlyList<PolygonCoordinate> ring)
    {
        var open = ring.Count > 1 && ring[0] == ring[^1] ? ring.Take(ring.Count - 1).ToList() : ring.ToList();
        if (open.Count <= MaxPointsPerRing)
            return open;

        var step = (double)open.Count / MaxPointsPerRing;
        var thinned = new List<PolygonCoordinate>(MaxPointsPerRing);
        for (var i = 0; i < MaxPointsPerRing; i++)
            thinned.Add(open[(int)(i * step)]);
        return thinned;
    }

    private static OneOf<List<IReadOnlyList<PolygonCoordinate>>, ShapeReadFailed> ReadGeoJson(byte[] content)
    {
        using var document = JsonDocument.Parse(content);
        var rings = new List<IReadOnlyList<PolygonCoordinate>>();
        CollectGeoJson(document.RootElement, rings);
        return rings;
    }

    private static void CollectGeoJson(JsonElement element, List<IReadOnlyList<PolygonCoordinate>> rings)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("type", out var typeElement))
            return;

        switch (typeElement.GetString())
        {
            case "FeatureCollection":
                if (element.TryGetProperty("features", out var features))
                    foreach (var feature in features.EnumerateArray())
                        CollectGeoJson(feature, rings);
                break;
            case "Feature":
                if (element.TryGetProperty("geometry", out var geometry))
                    CollectGeoJson(geometry, rings);
                break;
            case "GeometryCollection":
                if (element.TryGetProperty("geometries", out var geometries))
                    foreach (var member in geometries.EnumerateArray())
                        CollectGeoJson(member, rings);
                break;
            case "Polygon":
                if (element.TryGetProperty("coordinates", out var polygon))
                    AddGeoJsonOuterRing(polygon, rings);
                break;
            case "MultiPolygon":
                if (element.TryGetProperty("coordinates", out var polygons))
                    foreach (var member in polygons.EnumerateArray())
                        AddGeoJsonOuterRing(member, rings);
                break;
        }
    }

    // GeoJSON positions are [longitude, latitude]; the first ring is the outer one.
    private static void AddGeoJsonOuterRing(JsonElement polygon, List<IReadOnlyList<PolygonCoordinate>> rings)
    {
        if (polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() == 0)
            return;

        var ring = new List<PolygonCoordinate>();
        foreach (var position in polygon[0].EnumerateArray())
        {
            if (position.GetArrayLength() < 2)
                continue;
            ring.Add(new PolygonCoordinate(position[1].GetDouble(), position[0].GetDouble()));
        }
        rings.Add(ring);
    }

    private static OneOf<List<IReadOnlyList<PolygonCoordinate>>, ShapeReadFailed> ReadKml(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        var document = XDocument.Parse(text);
        var rings = new List<IReadOnlyList<PolygonCoordinate>>();

        // Namespace-agnostic: KML files declare several namespaces and some none.
        foreach (var polygon in document.Descendants().Where(e => e.Name.LocalName == "Polygon"))
        {
            var coordinates = polygon
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs")?
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "coordinates");
            if (coordinates is null)
                continue;

            var ring = new List<PolygonCoordinate>();
            var tuples = coordinates.Value.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var tuple in tuples)
            {
                var parts = tuple.Split(',');
                if (parts.Length < 2)
                    continue;
                var longitude = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                var latitude = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                ring.Add(new PolygonCoordinate(latitude, longitude));
            }
            rings.Add(ring);
        }

        return rings;
    }

    private static OneOf<List<IReadOnlyList<PolygonCoordinate>>, ShapeReadFailed> ReadZip(byte[] content)
    {
        using var archive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        var shape = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
        if (shape is null)
            return new ShapeReadFailed(ShapeReadFailure.NoPolygon);

        var projectionEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".prj", StringComparison.OrdinalIgnoreCase));
        string? projection = null;
        if (projectionEntry is not null)
        {
            using var reader = new StreamReader(projectionEntry.Open());
            projection = reader.ReadToEnd();
        }

        using var buffer = new MemoryStream();
        using var stream = shape.Open();
        stream.CopyTo(buffer);
        var shp = buffer.ToArray();
        return ReadShapefile(shp, projection);
    }

    // ESRI Shapefile: a 100-byte header, then records of a big-endian header and
    // little-endian content. Polygon content is a box, part offsets, then points.
    private static OneOf<List<IReadOnlyList<PolygonCoordinate>>, ShapeReadFailed> ReadShapefile(byte[] shp, string? projection)
    {
        // A projected CRS means the numbers are metres, not degrees.
        if (projection is not null && projection.Contains("PROJCS", StringComparison.OrdinalIgnoreCase))
            return new ShapeReadFailed(ShapeReadFailure.NotWgs84);

        int Int32BigEndian(int at) => BinaryPrimitives.ReadInt32BigEndian(shp.AsSpan(at, 4));
        int Int32LittleEndian(int at) => BinaryPrimitives.ReadInt32LittleEndian(shp.AsSpan(at, 4));
        double DoubleLittleEndian(int at) => BinaryPrimitives.ReadDoubleLittleEndian(shp.AsSpan(at, 8));

        if (shp.Length < ShapefileHeaderBytes || Int32BigEndian(0) != ShapefileMagic)
            return new ShapeReadFailed(ShapeReadFailure.Unreadable);

        var clockwise = new List<IReadOnlyList<PolygonCoordinate>>();
        var all = new List<IReadOnlyList<PolygonCoordinate>>();

        var offset = ShapefileHeaderBytes;
        while (offset + 8 <= shp.Length)
        {
            var contentWords = Int32BigEndian(offset + 4);
            var recordStart = offset + 8;
            var recordEnd = recordStart + contentWords * 2;
            if (recordEnd > shp.Length || contentWords < 2)
                break;

            var shapeType = Int32LittleEndian(recordStart);
            if (shapeType is 5 or 15 or 25)
            {
                var numParts = Int32LittleEndian(recordStart + 36);
                var numPoints = Int32LittleEndian(recordStart + 40);
                var partsOffset = recordStart + 44;
                var pointsOffset = partsOffset + numParts * 4;

                for (var part = 0; part < numParts; part++)
                {
                    var first = Int32LittleEndian(partsOffset + part * 4);
                    var last = part + 1 < numParts ? Int32LittleEndian(partsOffset + (part + 1) * 4) : numPoints;

                    var ring = new List<PolygonCoordinate>(Math.Max(0, last - first));
                    for (var i = first; i < last; i++)
                    {
                        var x = DoubleLittleEndian(pointsOffset + i * 16);
                        var y = DoubleLittleEndian(pointsOffset + i * 16 + 8);
                        ring.Add(new PolygonCoordinate(y, x));
                    }

                    all.Add(ring);
                    if (SignedArea(ring) < 0)
                        clockwise.Add(ring);
                }
            }

            offset = recordEnd;
        }

        // The spec makes outer rings clockwise and holes counter-clockwise, but
        // some writers ignore that; a file with no clockwise ring keeps them all.
        return clockwise.Count > 0 ? clockwise : all;
    }

    private static double SignedArea(IReadOnlyList<PolygonCoordinate> ring)
    {
        var area = 0.0;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            area += ring[j].Longitude * ring[i].Latitude - ring[i].Longitude * ring[j].Latitude;
        return area / 2;
    }
}
