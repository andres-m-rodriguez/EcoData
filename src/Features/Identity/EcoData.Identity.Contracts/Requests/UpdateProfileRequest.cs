using FluentValidation;

namespace EcoData.Identity.Contracts.Requests;

public sealed record UpdateProfileRequest(string DisplayName);

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(static x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MinimumLength(2)
            .WithMessage("Display name must be at least 2 characters")
            .MaximumLength(200)
            .WithMessage("Display name must be 200 characters or less");
    }
}
