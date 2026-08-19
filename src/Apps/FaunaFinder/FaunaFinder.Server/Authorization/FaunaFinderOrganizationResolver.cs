using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.Extensions.Options;

namespace FaunaFinder.Server.Authorization;

/// <summary>
/// Reads the configured organization once, before the app serves traffic.
/// </summary>
public sealed class FaunaFinderOrganizationResolver(
    IServiceScopeFactory scopeFactory,
    IOptions<FaunaFinderOptions> options,
    FaunaFinderOrganizationAccessor accessor,
    ILogger<FaunaFinderOrganizationResolver> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var slug = options.Value.OrganizationSlug;

        if (string.IsNullOrWhiteSpace(slug))
        {
            logger.LogWarning(
                "No {Section}:{Key} configured. Contributor permissions will be denied.",
                FaunaFinderOptions.SectionName,
                nameof(FaunaFinderOptions.OrganizationSlug)
            );

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var organizations = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var resolved = await organizations.GetBySlugAsync(slug, cancellationToken);

        if (resolved is null)
        {
            logger.LogWarning(
                "Organization '{Slug}' was not found. Contributor permissions will be denied "
                    + "until it exists.",
                slug
            );

            return;
        }

        accessor.Organization = new FaunaFinderOrganization(
            resolved.Id,
            resolved.Slug,
            resolved.Name,
            resolved.Tagline,
            resolved.ProfilePictureUrl,
            resolved.WebsiteUrl,
            resolved.PrimaryColor,
            resolved.AccentColor
        );

        logger.LogInformation(
            "FaunaFinder contributes to {Name} ('{Slug}', {OrganizationId}).",
            resolved.Name,
            resolved.Slug,
            resolved.Id
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
