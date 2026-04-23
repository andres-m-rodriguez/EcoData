using EcoData.Common.i18n;

namespace EcoData.Wildlife.Contracts.Dtos;

public sealed record SpeciesDtoForList(
    Guid Id,
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string GRank,
    string SRank,
    bool HasProfileImage,
    bool IsEndemic,
    IucnStatus? IucnStatus,
    string? TaxonCode,
    int MunicipalityCount,
    DateTimeOffset? LastObservedAtUtc,
    bool IsFeatured
);

public sealed record SpeciesDtoForDetail(
    Guid Id,
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string ElCode,
    string GRank,
    string SRank,
    string? ImageSourceUrl,
    bool HasProfileImage,
    IReadOnlyList<SpeciesCategoryDtoForList> Categories,
    IReadOnlyList<Guid> MunicipalityIds,
    bool IsEndemic,
    IucnStatus? IucnStatus,
    string? Habitat,
    DateTimeOffset? LastObservedAtUtc
);

public sealed record SpeciesDtoForCreate(
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string ElCode,
    string GRank,
    string SRank,
    string? ImageSourceUrl,
    byte[]? ProfileImageData,
    string? ProfileImageContentType,
    bool IsEndemic,
    IucnStatus? IucnStatus,
    bool IsFeatured,
    string? Habitat
);

public sealed record SpeciesDtoForUpdate(
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string ElCode,
    string GRank,
    string SRank,
    string? ImageSourceUrl,
    bool IsEndemic,
    IucnStatus? IucnStatus,
    bool IsFeatured,
    string? Habitat
);

public sealed record SpeciesStatsDto(
    int TotalSpecies,
    int EndemicCount,
    int ThreatenedCount,
    int MunicipalitiesCovered,
    int TotalMunicipalities,
    int AddedThisQuarter,
    int ReclassifiedThisQuarter
);

public sealed record TaxonFacetDto(string Code, int Count);

public sealed record IucnFacetDto(IucnStatus Status, int Count);

public sealed record SpeciesFacetsDto(
    IReadOnlyList<TaxonFacetDto> Taxa,
    IReadOnlyList<IucnFacetDto> Statuses,
    int EndemicCount,
    int RecentlyObservedCount,
    int WithImageCount
);
