using EcoData.Wildlife.Contracts.Dtos;
using FluentValidation;

namespace EcoData.Wildlife.Contracts.Validators;

public sealed class SightingNoteDtoForCreateValidator : AbstractValidator<SightingNoteDtoForCreate>
{
    public SightingNoteDtoForCreateValidator()
    {
        RuleFor(static note => note.Text)
            .NotEmpty()
            .WithMessage("Note text is required")
            .MaximumLength(2000)
            .WithMessage("Note text must be 2000 characters or less");
    }
}
