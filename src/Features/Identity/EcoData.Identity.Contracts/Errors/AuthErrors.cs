namespace EcoData.Identity.Contracts.Errors;

public sealed record InvalidCredentials;

public sealed record EmailAlreadyExists;

public sealed record AccountLocked;

public sealed record TooManyRequests(int RetryAfterMinutes);

public sealed record ValidationFailed(IReadOnlyList<string> Errors);
