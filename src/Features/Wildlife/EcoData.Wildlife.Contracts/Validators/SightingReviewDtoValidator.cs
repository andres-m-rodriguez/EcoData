using EcoData.Wildlife.Contracts.Dtos;
using FluentValidation;

namespace EcoData.Wildlife.Contracts.Validators;

public sealed class SightingReviewDtoValidator : AbstractValidator<SightingReviewDto>
{
    public SightingReviewDtoValidator()
    {
        RuleFor(static review => review.Reason)
            .MaximumLength(1000)
            .WithMessage("Reason must be 1000 characters or less");
    }
}
