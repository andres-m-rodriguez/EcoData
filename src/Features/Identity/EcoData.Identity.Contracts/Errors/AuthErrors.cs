namespace EcoData.Identity.Contracts.Errors;

public sealed record InvalidCredentials;

public sealed record InvalidPassword;

public sealed record EmailAlreadyExists;

public sealed record AccountLocked;

public sealed record TooManyRequests(int RetryAfterMinutes);

/// <summary>
/// Per-field validation errors, mirroring the RFC 9457 "errors" extension map.
/// Deliberately duplicated per feature to keep slices independent.
/// </summary>
public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Every validation message flattened, for UIs that render a flat list instead of per-field errors.</summary>
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();
}
