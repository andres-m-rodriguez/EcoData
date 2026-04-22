using FluentValidation;

namespace EcoData.Identity.Contracts.Requests;

public sealed record UpdateEmailRequest(string NewEmail, string CurrentPassword);

public sealed class UpdateEmailRequestValidator : AbstractValidator<UpdateEmailRequest>
{
    public UpdateEmailRequestValidator()
    {
        RuleFor(static x => x.NewEmail)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(256)
            .WithMessage("Email must be 256 characters or less");

        RuleFor(static x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");
    }
}
