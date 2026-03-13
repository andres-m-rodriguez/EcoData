namespace EcoData.Common.Results;

public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    private Error(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        Code = code;
        Message = message;
        Metadata = metadata;
    }

    public static Error Create(string code, string message) => new(code, message);

    public static Error Create(
        string code,
        string message,
        IReadOnlyDictionary<string, object> metadata
    ) => new(code, message, metadata);

    public Error WithMetadata(string key, object value)
    {
        var newMetadata = Metadata is not null
            ? new Dictionary<string, object>(Metadata) { [key] = value }
            : new Dictionary<string, object> { [key] = value };

        return new Error(Code, Message, newMetadata);
    }

    public override string ToString() => $"{Code}: {Message}";
}

public static class CommonErrors
{
    public static Error NotFound(string resource, object id) =>
        Error
            .Create("NotFound", $"{resource} with id '{id}' was not found.")
            .WithMetadata("Resource", resource)
            .WithMetadata("Id", id);

    public static Error Validation(string message) => Error.Create("Validation", message);

    public static Error Conflict(string message) => Error.Create("Conflict", message);

    public static Error Unauthorized(string message = "Unauthorized access.") =>
        Error.Create("Unauthorized", message);

    public static Error Forbidden(string message = "Access is forbidden.") =>
        Error.Create("Forbidden", message);

    public static Error Internal(string message = "An internal error occurred.") =>
        Error.Create("Internal", message);

    public static Error External(string service, string message) =>
        Error.Create("External", message).WithMetadata("Service", service);
}
