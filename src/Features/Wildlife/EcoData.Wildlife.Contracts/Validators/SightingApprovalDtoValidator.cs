using EcoData.Wildlife.Contracts.Dtos;
using FluentValidation;

namespace EcoData.Wildlife.Contracts.Validators;

public sealed class SightingApprovalDtoValidator : AbstractValidator<SightingApprovalDto>
{
    public SightingApprovalDtoValidator()
    {
        RuleFor(static approval => approval.Reason)
            .MaximumLength(1000)
            .WithMessage("Reason must be 1000 characters or less");
    }
}
