using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.Extensions.Options;

namespace FaunaFinder.Server.Authorization;

/// <summary>
/// Reads the configured organization. Says nothing about when it runs — the host decides
/// whether that is once at startup, lazily on first use, or on a refresh.
/// </summary>
public sealed class FaunaFinderOrganizationResolver(
    IOptions<FaunaFinderOptions> options,
    IOrganizationRepository organizations,
    ILogger<FaunaFinderOrganizationResolver> logger
)
{
    /// <returns>
    /// The organization, or <see langword="null"/> when none is configured or the
    /// configured slug matches nothing. Both cases are logged and neither throws:
    /// FaunaFinder is a public catalogue first and has to keep serving anonymous traffic.
    /// </returns>
    public async Task<FaunaFinderOrganization?> ResolveAsync(
        CancellationToken cancellationToken = default
    )
    {
        var slug = options.Value.OrganizationSlug;

        if (string.IsNullOrWhiteSpace(slug))
        {
            logger.LogWarning(
                "No {Section}:{Key} configured. Contributor permissions will be denied.",
                FaunaFinderOptions.SectionName,
                nameof(FaunaFinderOptions.OrganizationSlug)
            );

            return null;
        }

        var resolved = await organizations.GetBySlugAsync(slug, cancellationToken);

        if (resolved is null)
        {
            logger.LogWarning(
                "Organization '{Slug}' was not found. Contributor permissions will be denied "
                    + "until it exists.",
                slug
            );

            return null;
        }

        logger.LogInformation(
            "FaunaFinder contributes to {Name} ('{Slug}', {OrganizationId}).",
            resolved.Name,
            resolved.Slug,
            resolved.Id
        );

        return new FaunaFinderOrganization(
            resolved.Id,
            resolved.Slug,
            resolved.Name,
            resolved.Tagline,
            resolved.ProfilePictureUrl,
            resolved.WebsiteUrl,
            resolved.PrimaryColor,
            resolved.AccentColor
        );
    }
}
