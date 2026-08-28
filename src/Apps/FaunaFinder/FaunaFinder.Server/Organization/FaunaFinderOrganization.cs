using EcoData.Organization.Contracts;

namespace FaunaFinder.Server.Organization;

public sealed record FaunaFinderOrganization(
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
    string? ContactEmail,
    OrganizationType? Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
