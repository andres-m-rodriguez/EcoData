using FluentValidation;

namespace EcoData.Sensors.Contracts.Dtos;

public sealed record ReadingBatchDtoForCreate(
    Guid SensorId,
    IReadOnlyList<ReadingItemDto> Readings
);

public sealed record ReadingItemDto(
    string Parameter,
    string? Description,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);

public sealed record ReadingBatchResult(
    int TotalSubmitted,
    int Accepted,
    int Rejected,
    IReadOnlyList<string> Errors
);

public sealed class ReadingItemValidator : AbstractValidator<ReadingItemDto>
{
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    public ReadingItemValidator(DateTimeOffset now)
    {
        var maxAllowedTime = now + FutureTolerance;
        var minAllowedTime = now - MaxAge;

        RuleFor(static x => x.Parameter)
            .NotEmpty()
            .WithMessage("Parameter is required");

        RuleFor(x => x.RecordedAt)
            .LessThanOrEqualTo(maxAllowedTime)
            .WithMessage(x => $"Timestamp {x.RecordedAt:O} is in the future")
            .GreaterThanOrEqualTo(minAllowedTime)
            .WithMessage(x => $"Timestamp {x.RecordedAt:O} is older than 30 days");
    }
}
