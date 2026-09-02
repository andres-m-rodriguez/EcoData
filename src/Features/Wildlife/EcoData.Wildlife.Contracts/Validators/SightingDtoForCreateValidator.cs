using EcoData.Wildlife.Contracts.Dtos;
using FluentValidation;

namespace EcoData.Wildlife.Contracts.Validators;

public sealed class SightingDtoForCreateValidator : AbstractValidator<SightingDtoForCreate>
{
    private static readonly DateTimeOffset EarliestObservation = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public SightingDtoForCreateValidator()
    {
        RuleFor(static sighting => sighting.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(static sighting => sighting.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");

        RuleFor(static sighting => sighting.ObservedAtUtc)
            .GreaterThanOrEqualTo(EarliestObservation)
            .WithMessage("Observation date must be after 1900")
            .LessThanOrEqualTo(static _ => DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("Observation date cannot be in the future");

        RuleFor(static sighting => sighting.IndividualCount)
            .InclusiveBetween(1, 10000)
            .When(static sighting => sighting.IndividualCount.HasValue)
            .WithMessage("Individual count must be between 1 and 10000");

        RuleFor(static sighting => sighting.Note)
            .MaximumLength(2000)
            .WithMessage("Note must be 2000 characters or less");
    }
}
