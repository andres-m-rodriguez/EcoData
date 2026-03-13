namespace EcoData.Organization.Contracts.Dtos;

public sealed record OrganizationDtoForList(
    Guid Id,
    string Name,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    DateTimeOffset CreatedAt
);

public sealed record OrganizationDtoForDetail(
    Guid Id,
    string Name,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record OrganizationDtoForCreate(
    string Name,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForUpdate(
    string Name,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForCreated(Guid Id, string Name);

public sealed record MyOrganizationDto(
    Guid Id,
    string Name,
    string? ProfilePictureUrl,
    string? WebsiteUrl,
    string RoleName
);
