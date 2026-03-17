using FluentValidation;

namespace EcoData.Sensors.Contracts.Requests;

public sealed record RegisterSensorRequest(
    Guid OrganizationId,
    string OrganizationName,
    string Name,
    string ExternalId,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    Guid? SensorTypeId = null,
    int? ExpectedIntervalSeconds = 300
);

public sealed class RegisterSensorRequestValidator : AbstractValidator<RegisterSensorRequest>
{
    public RegisterSensorRequestValidator()
    {
        RuleFor(static x => x.OrganizationId).NotEmpty().WithMessage("Organization is required");

        RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(300)
            .WithMessage("Name must be 300 characters or less");

        RuleFor(static x => x.ExternalId)
            .NotEmpty()
            .WithMessage("External ID is required")
            .MaximumLength(100)
            .WithMessage("External ID must be 100 characters or less");

        RuleFor(static x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(static x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude must be between -180 and 180");

        RuleFor(static x => x.MunicipalityId).NotEmpty().WithMessage("Municipality is required");

        RuleFor(static x => x.ExpectedIntervalSeconds)
            .InclusiveBetween(10, 86400)
            .When(static x => x.ExpectedIntervalSeconds.HasValue)
            .WithMessage("Expected interval must be between 10 seconds and 24 hours");
    }
}
