using System.Text.Json.Serialization;

namespace EcoData.Wildlife.Contracts.Parameters;

// OrganizationId names the membership whose grants decide whether areas come back.
public sealed record NearbySpeciesParameters(
    double Latitude,
    double Longitude,
    double RadiusMeters = 5000,
    Guid? OrganizationId = null
);

public sealed record PolygonCoordinate(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude
);

public sealed record PolygonSearchParameters(
    [property: JsonPropertyName("coordinates")] IReadOnlyList<PolygonCoordinate> Coordinates,
    [property: JsonPropertyName("organizationId")] Guid? OrganizationId = null
);
