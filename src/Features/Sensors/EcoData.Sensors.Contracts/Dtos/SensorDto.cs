using FluentValidation;

namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorDtoForList(
    Guid Id,
    Guid OrganizationId,
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive,
    string? DataSourceName
);

public sealed record SensorDtoForDetail(
    Guid Id,
    Guid OrganizationId,
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? DataSourceName
);

public sealed record SensorDtoForCreate(
    Guid? SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive
);

public sealed record SensorDtoForCreated(Guid Id, string ExternalId);

public sealed record SensorDtoForUpdate(
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Guid MunicipalityId,
    bool IsActive
);

public sealed class SensorDtoForUpdateValidator : AbstractValidator<SensorDtoForUpdate>
{
    public SensorDtoForUpdateValidator()
    {
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

        RuleFor(static x => x.MunicipalityId)
            .NotEmpty()
            .WithMessage("Municipality is required");
    }
}

public sealed record SensorRegistrationResultDto(
    Guid SensorId,
    string AccessToken,
    DateTimeOffset ExpiresAt
);
