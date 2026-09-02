using EcoData.Wildlife.Contracts.Dtos;
using FluentValidation;

namespace EcoData.Wildlife.Contracts.Validators;

public sealed class SightingDenialDtoValidator : AbstractValidator<SightingDenialDto>
{
    public SightingDenialDtoValidator()
    {
        RuleFor(static denial => denial.Reason)
            .NotEmpty()
            .WithMessage("A reason is required to deny a sighting")
            .MaximumLength(1000)
            .WithMessage("Reason must be 1000 characters or less");
    }
}
