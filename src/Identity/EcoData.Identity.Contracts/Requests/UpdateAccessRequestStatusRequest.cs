using FluentValidation;

namespace EcoData.Identity.Contracts.Requests;

public sealed record UpdateAccessRequestStatusRequest(
    bool Approved,
    string? ReviewNotes = null
);

public sealed class UpdateAccessRequestStatusRequestValidator : AbstractValidator<UpdateAccessRequestStatusRequest>
{
    public UpdateAccessRequestStatusRequestValidator()
    {
        RuleFor(static x => x.ReviewNotes)
            .MaximumLength(1000).WithMessage("Review notes must be 1000 characters or less");
    }
}
