using EcoData.Common.i18n;

namespace EcoData.Wildlife.Contracts.Dtos;

public sealed record FwsActionDtoForList(
    Guid Id,
    string Code,
    IReadOnlyList<LocaleValue> Name
);

public sealed record NrcsPracticeDtoForList(
    Guid Id,
    string Code,
    IReadOnlyList<LocaleValue> Name
);

public sealed record FwsLinkDtoForDetail(
    Guid Id,
    Guid SpeciesId,
    FwsActionDtoForList FwsAction,
    NrcsPracticeDtoForList NrcsPractice,
    IReadOnlyList<LocaleValue> Justification
);

public sealed record ConservationLinksDtoForSpecies(
    IReadOnlyList<FwsLinkDtoForDetail> Links
);
