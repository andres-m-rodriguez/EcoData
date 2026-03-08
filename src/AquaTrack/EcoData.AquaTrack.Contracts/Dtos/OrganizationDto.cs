namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record OrganizationDtoForList(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt
);

public sealed record OrganizationDtoForDetail(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record OrganizationDtoForCreate(
    string Name
);

public sealed record OrganizationDtoForUpdate(
    string Name
);

public sealed record OrganizationDtoForCreated(
    Guid Id,
    string Name
);
