using EcoData.Common.i18n;

namespace EcoData.Wildlife.Contracts.Dtos;

public sealed record SpeciesDtoForList(
    Guid Id,
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string GRank,
    string SRank
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
    IReadOnlyList<Guid> MunicipalityIds
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
    string? ProfileImageContentType
);

public sealed record SpeciesDtoForUpdate(
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string ElCode,
    string GRank,
    string SRank,
    string? ImageSourceUrl
);
