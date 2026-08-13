using EcoData.Common.i18n;

namespace EcoData.Wildlife.Mcp;

// The shapes the tools hand back to a model, deliberately narrower than the
// DTOs the web app reads. A tool result is spent from a context window, so the
// list shapes carry only what a model needs to decide whether to ask for the
// detail — the full record is one `get_species` call away.
//
// Names arrive from the repositories as a per-locale list; these carry the one
// resolved string instead, so the model never has to pick a translation.

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

/// <summary>
/// How many species one municipio has on record. Carries the id rather than the
/// name: the municipio list lives in the locations feature, and a model that
/// wants names has it in one call from there.
/// </summary>
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

/// <summary>
/// Turning stored wildlife records into the shapes above.
/// </summary>
internal static class WildlifeMcpMapping
{
    private const string DefaultLocale = "en";

    /// <summary>
    /// The one name to show. Prefers English, falls back to whatever locale the
    /// record does carry, and finally to the scientific name — a species with no
    /// common name at all still has to read as something.
    /// </summary>
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

    // "fauna"/"flora" rather than the stored boolean: the model reads the
    // answer instead of having to know which way the flag points.
    internal static string Kind(bool isFauna) => isFauna ? "fauna" : "flora";
}
