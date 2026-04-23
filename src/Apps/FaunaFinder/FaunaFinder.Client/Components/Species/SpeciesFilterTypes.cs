namespace FaunaFinder.Client.Components.Species;

public enum FaunaFilter
{
    All,
    Fauna,
    Flora,
}

public sealed record SpeciesFilterResult(Guid? CategoryId, FaunaFilter Fauna);
