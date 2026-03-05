namespace EcoData.Identity.Contracts.Errors;

public sealed record InvalidCredentials;

public sealed record EmailAlreadyExists;

public sealed record AccountLocked;

public sealed record AccountNotApproved;

public sealed record PendingAccessRequest;

public sealed record AccessRequestNotFound;

public sealed record AccessRequestAlreadyProcessed;

public sealed record ValidationFailed(IReadOnlyList<string> Errors);
