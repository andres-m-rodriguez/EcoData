using FluentValidation;

namespace EcoData.Identity.Contracts.Requests;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password,
    string ConfirmPassword,
    Guid OrganizationId,
    string OrganizationName
);

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(static x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(256)
            .WithMessage("Email must be 256 characters or less");

        RuleFor(static x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MinimumLength(2)
            .WithMessage("Display name must be at least 2 characters")
            .MaximumLength(200)
            .WithMessage("Display name must be 200 characters or less");

        RuleFor(static x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .MaximumLength(100)
            .WithMessage("Password must be 100 characters or less");

        RuleFor(static x => x.ConfirmPassword)
            .Equal(static x => x.Password)
            .WithMessage("Passwords do not match");

        RuleFor(static x => x.OrganizationId).NotEmpty().WithMessage("Organization is required");

        RuleFor(static x => x.OrganizationName)
            .NotEmpty()
            .WithMessage("Organization name is required")
            .MaximumLength(200)
            .WithMessage("Organization name must be 200 characters or less");
    }
}
