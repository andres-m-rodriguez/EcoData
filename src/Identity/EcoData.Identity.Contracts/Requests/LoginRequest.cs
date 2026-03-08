using FluentValidation;

namespace EcoData.Identity.Contracts.Requests;

public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(static x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(static x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}
