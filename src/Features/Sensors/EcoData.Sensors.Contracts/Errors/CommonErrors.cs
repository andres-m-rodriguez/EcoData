namespace EcoData.Sensors.Contracts.Errors;

public sealed record NotFoundError;

public sealed record ValidationError(IReadOnlyList<ValidationFailure> Errors)
{
    public ValidationError() : this(Array.Empty<ValidationFailure>()) { }

    public ValidationError(string message) : this([new ValidationFailure("", message)]) { }
}

public sealed record ValidationFailure(string PropertyName, string ErrorMessage);

public sealed record ApiError(int StatusCode, string? Message = null);

public sealed record ConflictError(string Message);

public sealed record ForbiddenError(string? Message = null);

public sealed record Success;

public sealed record UnauthorizedError;
