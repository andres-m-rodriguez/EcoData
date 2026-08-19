using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.Extensions.Options;

namespace FaunaFinder.Server.Authorization;

/// <summary>
/// Turns the configured slug into an organization id once, before the app serves traffic.
/// </summary>
public sealed class FaunaFinderOrganizationResolver(
    IServiceScopeFactory scopeFactory,
    IOptions<FaunaFinderOptions> options,
    FaunaFinderOrganization organization,
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

        organization.Id = resolved.Id;

        logger.LogInformation(
            "FaunaFinder contributes to organization '{Slug}' ({OrganizationId}).",
            slug,
            resolved.Id
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
