namespace EcoData.Locations.Contracts.Dtos;

public sealed record StateDtoForList(
    Guid Id,
    string Name,
    string Code,
    string FipsCode
);

public sealed record StateDtoForDetail(
    Guid Id,
    string Name,
    string Code,
    string FipsCode,
    DateTimeOffset CreatedAt
);
