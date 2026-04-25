namespace EcoData.Organization.Contracts.Dtos;

public sealed record OrganizationDtoForList(
    Guid Id,
    string Name,
    string Slug,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    DateTimeOffset CreatedAt
);

public sealed record OrganizationDtoForDetail(
    Guid Id,
    string Name,
    string Slug,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// Slug is optional on create/update — when null/empty the repository derives
// it from Name and ensures uniqueness by appending a numeric suffix on collision.
public sealed record OrganizationDtoForCreate(
    string Name,
    string? Slug = null,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForUpdate(
    string Name,
    string? Slug = null,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null
);

public sealed record OrganizationDtoForCreated(Guid Id, string Name, string Slug);

public sealed record MyOrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string? ProfilePictureUrl,
    string? WebsiteUrl,
    string RoleName
);
