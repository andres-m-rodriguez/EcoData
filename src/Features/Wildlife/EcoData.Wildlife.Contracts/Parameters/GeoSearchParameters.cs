using System.Text.Json.Serialization;

namespace EcoData.Wildlife.Contracts.Parameters;

public sealed record NearbySpeciesParameters(
    double Latitude,
    double Longitude,
    double RadiusMeters = 5000
);

public sealed record PolygonCoordinate(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude
);

public sealed record PolygonSearchParameters(
    [property: JsonPropertyName("coordinates")] IReadOnlyList<PolygonCoordinate> Coordinates
);
