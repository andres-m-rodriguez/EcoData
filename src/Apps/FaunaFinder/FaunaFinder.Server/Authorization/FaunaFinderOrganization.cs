namespace FaunaFinder.Server.Authorization;

/// <summary>
/// The organization FaunaFinder contributes to, read once at startup and held in memory.
/// </summary>
/// <remarks>
/// Carries more than the id because the rest is useful to the app and free once resolved —
/// the name and branding for "contributing to…" surfaces, the slug for links back to the
/// organization page. Fields the catalogue has no business holding (tax id, legal status)
/// are deliberately not copied across.
/// </remarks>
public sealed record FaunaFinderOrganization(
    Guid Id,
    string Slug,
    string Name,
    string? Tagline,
    string? ProfilePictureUrl,
    string? WebsiteUrl,
    string? PrimaryColor,
    string? AccentColor
);
