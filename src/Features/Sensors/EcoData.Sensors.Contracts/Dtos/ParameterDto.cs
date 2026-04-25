namespace EcoData.Sensors.Contracts.Dtos;

public sealed record ParameterDtoForList(
    Guid Id,
    Guid? SourceId,
    string Code,
    string Name,
    string DefaultUnit,
    Guid? SensorTypeId,
    string? SensorTypeName,
    Guid PhenomenonId,
    string PhenomenonCode,
    string SourceUnit,
    double UnitFactor,
    double UnitOffset,
    string ValueShape
);

public sealed record ParameterDtoForDetail(
    Guid Id,
    Guid? SourceId,
    string Code,
    string Name,
    string DefaultUnit,
    Guid? SensorTypeId,
    string? SensorTypeName,
    Guid PhenomenonId,
    string PhenomenonCode,
    string SourceUnit,
    double UnitFactor,
    double UnitOffset,
    string ValueShape,
    DateTimeOffset CreatedAt
);
