namespace EcoData.Identity.Contracts.Errors;

public sealed record InvalidCredentials;

public sealed record InvalidPassword;

public sealed record EmailAlreadyExists;

public sealed record AccountLocked;

public sealed record TooManyRequests(int RetryAfterMinutes);

/// <summary>The server rejected the request with per-field validation errors.</summary>
public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();
}
