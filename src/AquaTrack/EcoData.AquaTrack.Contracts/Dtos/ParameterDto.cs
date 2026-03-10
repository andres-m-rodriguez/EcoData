namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record ParameterDtoForList(
    Guid Id,
    string Code,
    string Name,
    string DefaultUnit,
    Guid SensorTypeId,
    string SensorTypeName
);

public sealed record ParameterDtoForDetail(
    Guid Id,
    string Code,
    string Name,
    string DefaultUnit,
    Guid SensorTypeId,
    string SensorTypeName,
    DateTimeOffset CreatedAt
);
