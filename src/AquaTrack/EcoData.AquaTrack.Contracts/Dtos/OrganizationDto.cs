namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record OrganizationDtoForList(
    Guid Id,
    string Name,
    string? ProfilePictureUrl,
    DateTimeOffset CreatedAt
);

public sealed record OrganizationDtoForDetail(
    Guid Id,
    string Name,
    string? ProfilePictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record OrganizationDtoForCreate(
    string Name,
    string? ProfilePictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForUpdate(
    string Name,
    string? ProfilePictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForCreated(
    Guid Id,
    string Name
);
