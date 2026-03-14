namespace EcoData.Sensors.Contracts.Requests;

public sealed record RegisterSensorRequest(
    Guid OrganizationId,
    string OrganizationName,
    string Name,
    string ExternalId,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    Guid? SensorTypeId = null
);
