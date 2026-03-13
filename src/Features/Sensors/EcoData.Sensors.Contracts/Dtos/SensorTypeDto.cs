namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorTypeDtoForList(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int ParameterCount
);

public sealed record SensorTypeDtoForDetail(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ParameterDtoForList> Parameters
);
