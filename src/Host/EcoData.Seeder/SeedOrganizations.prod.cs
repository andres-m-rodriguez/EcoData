using EcoData.Organization.Contracts;
using EcoData.Organization.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Seeder;

// Organizations every environment carries. The .dev.cs half adds the ones that
// only exist to give the dev accounts somewhere to be.
internal sealed partial class SeedOrganizations(
    OrganizationDbContext context,
    ILogger<SeedOrganizations> logger
)
{
    // FaunaFinder is bound to this slug (FaunaFinderOrganizationLoader); keep them in sync.
    public const string InterMetroSlug = "inter-metro";

    public Task SeedAsync(CancellationToken ct) =>
        EnsureAsync(InterMetroSlug, "Inter Metro University", OrganizationType.University, ct);

    private async Task EnsureAsync(
        string slug,
        string name,
        OrganizationType type,
        CancellationToken ct
    )
    {
        if (await context.Organizations.AnyAsync(o => o.Slug == slug, ct))
            return;

        logger.LogInformation("Creating organization '{Slug}'...", slug);

        var now = DateTimeOffset.UtcNow;
        context.Organizations.Add(
            new Organization.Database.Models.Organization
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Slug = slug,
                Tagline = null,
                ProfilePictureUrl = null,
                CardPictureUrl = null,
                AboutUs = null,
                WebsiteUrl = null,
                Location = null,
                FoundedYear = null,
                LegalStatus = null,
                TaxId = null,
                PrimaryColor = null,
                AccentColor = null,
                ContactEmail = null,
                Type = type,
                CreatedAt = now,
                UpdatedAt = now,
            }
        );
        await context.SaveChangesAsync(ct);
    }
}
