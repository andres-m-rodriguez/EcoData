using EcoData.Wildlife.Contracts;

namespace FaunaFinder.Client.Components.Species;

public enum FaunaFilter
{
    All,
    Fauna,
    Flora,
}

public sealed record SpeciesFilterResult(
    IReadOnlyList<string> TaxonCodes,
    IReadOnlyList<IucnStatus> IucnStatuses,
    bool EndemicOnly,
    bool ObservedRecently,
    bool HasPhoto,
    int? MinMunicipalityCount
)
{
    public static SpeciesFilterResult Empty { get; } = new([], [], false, false, false, null);
}
