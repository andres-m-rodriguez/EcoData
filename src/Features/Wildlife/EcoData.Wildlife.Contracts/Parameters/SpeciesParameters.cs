using EcoData.Common.Pagination;

namespace EcoData.Wildlife.Contracts.Parameters;

public sealed record SpeciesParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? MunicipalityId = null,
    bool? IsFauna = null,
    bool? IsEndemic = null,
    bool? HasProfileImage = null,
    IReadOnlyList<IucnStatus>? IucnStatuses = null,
    IReadOnlyList<string>? TaxonCodes = null,
    int? MinMunicipalityCount = null,
    DateTimeOffset? ObservedSinceUtc = null,
    SpeciesSort Sort = SpeciesSort.ScientificNameAsc
) : CursorParameters(PageSize, Cursor);

public enum SpeciesSort
{
    ScientificNameAsc,
    ScientificNameDesc,
    RecentlyObserved,
    MostMunicipalities,
}
