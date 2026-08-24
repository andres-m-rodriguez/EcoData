namespace FaunaFinder.Client.Components.Species;

public static class SpeciesNaming
{
    public static bool EchoesScientificName(string? commonName, string? scientificName) =>
        string.Equals(
            commonName?.Trim(),
            scientificName?.Trim(),
            StringComparison.OrdinalIgnoreCase
        );
}
