namespace EcoData.Sensors.Contracts.Dtos;

public sealed record PhenomenonDtoForList(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string CanonicalUnit,
    string DefaultValueShape,
    IReadOnlyList<string> Capabilities
);

public sealed record PhenomenonDtoForDetail(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string CanonicalUnit,
    string DefaultValueShape,
    IReadOnlyList<string> Capabilities,
    DateTimeOffset CreatedAt,
    int ParameterCount
);
