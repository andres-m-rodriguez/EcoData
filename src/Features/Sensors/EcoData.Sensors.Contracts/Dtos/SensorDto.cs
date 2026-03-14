namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorDtoForList(
    Guid Id,
    Guid OrganizationId,
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive,
    string? DataSourceName
);

public sealed record SensorDtoForDetail(
    Guid Id,
    Guid OrganizationId,
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? DataSourceName
);

public sealed record SensorDtoForCreate(
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive
);

public sealed record SensorDtoForCreated(Guid Id, string ExternalId);

public sealed record SensorDtoForUpdate(
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive
);

public sealed record SensorRegistrationResultDto(
    Guid SensorId,
    string AccessToken,
    DateTimeOffset ExpiresAt
);
