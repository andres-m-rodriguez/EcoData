namespace EcoData.Organization.Contracts.Dtos;

public sealed record OrganizationDtoForList(
    Guid Id,
    string Name,
    string Slug,
    string? Tagline,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    string? Location,
    string? PrimaryColor,
    string? AccentColor,
    DateTimeOffset CreatedAt
);

public sealed record OrganizationDtoForDetail(
    Guid Id,
    string Name,
    string Slug,
    string? Tagline,
    string? ProfilePictureUrl,
    string? CardPictureUrl,
    string? AboutUs,
    string? WebsiteUrl,
    string? Location,
    int? FoundedYear,
    string? LegalStatus,
    string? TaxId,
    string? PrimaryColor,
    string? AccentColor,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// Slug is optional on create/update — when null/empty the repository derives
// it from Name and ensures uniqueness by appending a numeric suffix on collision.
public sealed record OrganizationDtoForCreate(
    string Name,
    string? Slug = null,
    string? Tagline = null,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null,
    string? Location = null,
    int? FoundedYear = null,
    string? LegalStatus = null,
    string? TaxId = null,
    string? PrimaryColor = null,
    string? AccentColor = null
);

public sealed record OrganizationDtoForUpdate(
    string Name,
    string? Slug = null,
    string? Tagline = null,
    string? ProfilePictureUrl = null,
    string? CardPictureUrl = null,
    string? AboutUs = null,
    string? WebsiteUrl = null,
    string? Location = null,
    int? FoundedYear = null,
    string? LegalStatus = null,
    string? TaxId = null,
    string? PrimaryColor = null,
    string? AccentColor = null
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
