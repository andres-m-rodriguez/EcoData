using EcoData.Common.i18n;

namespace EcoData.Wildlife.Contracts.Dtos;

public sealed record SightingDtoForCreate(
    Guid SpeciesId,
    double Latitude,
    double Longitude,
    Guid? MunicipalityId,
    DateTimeOffset ObservedAtUtc,
    int? IndividualCount,
    string? Note
);

public sealed record SightingDto(
    Guid Id,
    Guid OrganizationId,
    Guid SpeciesId,
    IReadOnlyList<LocaleValue> SpeciesCommonName,
    string SpeciesScientificName,
    double Latitude,
    double Longitude,
    Guid? MunicipalityId,
    DateTimeOffset ObservedAtUtc,
    int? IndividualCount,
    SightingStatus Status,
    Guid ReporterUserId,
    string ReporterDisplayName,
    string? ReviewedByDisplayName,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewReason,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<SightingNoteDto> Notes,
    IReadOnlyList<SightingImageDto> Images
);

public sealed record SightingNoteDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Text,
    DateTimeOffset CreatedAtUtc
);

public sealed record SightingNoteDtoForCreate(string Text);

public sealed record SightingReviewDto(string? Reason);

public sealed record SightingImageDto(
    Guid Id,
    string ContentType,
    long SizeBytes,
    string UploadedByDisplayName,
    DateTimeOffset CreatedAtUtc
);
