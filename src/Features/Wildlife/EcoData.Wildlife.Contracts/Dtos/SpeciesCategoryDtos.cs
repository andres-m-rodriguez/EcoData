using EcoData.Common.i18n;

namespace EcoData.Wildlife.Contracts.Dtos;

public sealed record SpeciesCategoryDtoForList(
    Guid Id,
    string Code,
    IReadOnlyList<LocaleValue> Name
);

public sealed record SpeciesCategoryDtoForDetail(
    Guid Id,
    string Code,
    IReadOnlyList<LocaleValue> Name,
    int SpeciesCount
);
