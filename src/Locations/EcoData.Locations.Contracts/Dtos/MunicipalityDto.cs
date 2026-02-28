namespace EcoData.Locations.Contracts.Dtos;

public sealed record MunicipalityDtoForList(
    Guid Id,
    string Name,
    string GeoJsonId,
    string CountyFipsCode,
    decimal CentroidLatitude,
    decimal CentroidLongitude
);

public sealed record MunicipalityDtoForDetail(
    Guid Id,
    Guid StateId,
    string StateName,
    string StateCode,
    string Name,
    string GeoJsonId,
    string CountyFipsCode,
    decimal CentroidLatitude,
    decimal CentroidLongitude,
    DateTimeOffset CreatedAt
);

public sealed record MunicipalityDtoForGeoJson(
    Guid Id,
    string Name,
    string GeoJsonId,
    decimal CentroidLatitude,
    decimal CentroidLongitude,
    string? BoundaryGeoJson
);
