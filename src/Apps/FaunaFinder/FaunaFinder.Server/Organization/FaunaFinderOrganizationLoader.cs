using System.Net;
using EcoData.Organization.Contracts.Dtos;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FaunaFinder.Server.Organization;

// The organization module lives in EcoPortal, so the FaunaFinder organization is
// resolved over HTTP at startup and cached here for the lifetime of the
// process. Current stays null while unresolved (fresh dev database has no
// organizations).
public sealed class FaunaFinderOrganizationLoader(
    IHttpClientFactory httpClientFactory,
    ILogger<FaunaFinderOrganizationLoader> logger
) : BackgroundService
{
    public const string HttpClientName = "ecoportal";
    private const string Slug = "inter-metro";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    public FaunaFinderOrganization? Current { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"organization/organizations/by-slug/{Slug}",
                    stoppingToken
                );

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogWarning(
                        "Organization '{Slug}' does not exist; FaunaFinder organization data is unavailable",
                        Slug
                    );
                    return;
                }

                if (response.IsSuccessStatusCode)
                {
                    var dto = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
                        stoppingToken
                    );
                    Current = new FaunaFinderOrganization(
                        dto!.Id,
                        dto.Name,
                        dto.Slug,
                        dto.Tagline,
                        dto.ProfilePictureUrl,
                        dto.CardPictureUrl,
                        dto.AboutUs,
                        dto.WebsiteUrl,
                        dto.Location,
                        dto.FoundedYear,
                        dto.LegalStatus,
                        dto.TaxId,
                        dto.PrimaryColor,
                        dto.AccentColor,
                        dto.ContactEmail,
                        dto.Type,
                        dto.CreatedAt,
                        dto.UpdatedAt
                    );
                    logger.LogInformation(
                        "Owner organization '{Slug}' resolved to {OrganizationId}",
                        Slug,
                        dto.Id
                    );
                    return;
                }

                logger.LogWarning(
                    "Resolving FaunaFinder organization '{Slug}' returned {StatusCode}; retrying",
                    Slug,
                    (int)response.StatusCode
                );
            }
            catch (Exception e)
                when (e is HttpRequestException
                        or TimeoutRejectedException
                        or BrokenCircuitException
                )
            {
                logger.LogWarning(
                    e,
                    "EcoPortal unreachable while resolving FaunaFinder organization '{Slug}'; retrying",
                    Slug
                );
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }
}
