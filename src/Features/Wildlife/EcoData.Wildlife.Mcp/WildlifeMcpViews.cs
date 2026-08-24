using EcoData.Common.i18n;

namespace EcoData.Wildlife.Mcp;

public sealed record SpeciesSummary(
    Guid Id,
    string CommonName,
    string ScientificName,
    string Kind,
    string? IucnStatus,
    string EndemicStatus,
    string? TaxonCode,
    int MunicipalityCount,
    DateTimeOffset? LastObservedAtUtc
);

public sealed record SpeciesDetail(
    Guid Id,
    string CommonName,
    string ScientificName,
    string Kind,
    string? IucnStatus,
    string EndemicStatus,
    string? Habitat,
    IReadOnlyList<string> Categories,
    int MunicipalityCount,
    DateTimeOffset? LastObservedAtUtc,
    string? ImageSourceUrl
);

public sealed record NearbySpecies(
    Guid Id,
    string CommonName,
    string ScientificName,
    string Kind,
    double DistanceMeters,
    string? LocationDescription
);

public sealed record MunicipalityRichness(Guid MunicipalityId, int SpeciesCount);

public sealed record TaxonCategory(string Code, string Name);

public sealed record ConservationPractice(string Code, string Name);

public sealed record RecoveryAction(string Code, string Name);

public sealed record CatalogueStats(
    int TotalSpecies,
    int EndemicCount,
    int ThreatenedCount,
    int MunicipalitiesCovered,
    int TotalMunicipalities
);

internal static class WildlifeMcpMapping
{
    private const string DefaultLocale = "en";

    internal static string ResolveName(
        IReadOnlyList<LocaleValue> localized,
        string fallback
    )
    {
        foreach (var value in localized)
        {
            if (string.Equals(value.Code, DefaultLocale, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(value.Value))
            {
                return value.Value;
            }
        }

        foreach (var value in localized)
        {
            if (!string.IsNullOrWhiteSpace(value.Value))
            {
                return value.Value;
            }
        }

        return fallback;
    }

    internal static string Kind(bool isFauna) => isFauna ? "fauna" : "flora";
}
