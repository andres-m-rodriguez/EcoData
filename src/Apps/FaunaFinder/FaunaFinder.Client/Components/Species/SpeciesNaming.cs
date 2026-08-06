namespace FaunaFinder.Client.Components.Species;

/// <summary>
/// Naming helpers shared by the species cards, panels and hero.
/// </summary>
public static class SpeciesNaming
{
    /// <summary>
    /// True when the resolved common name adds nothing over the scientific name,
    /// so the caller can render one line instead of the same text twice. Species
    /// the matching matrix marks as having no common name fall back to the
    /// scientific name, and it echoes the binomial outright for a few others.
    /// </summary>
    public static bool EchoesScientificName(string? commonName, string? scientificName) =>
        string.Equals(
            commonName?.Trim(),
            scientificName?.Trim(),
            StringComparison.OrdinalIgnoreCase
        );
}
