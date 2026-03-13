namespace EcoData.Organization.Contracts.Dtos;

public sealed record ApiKeyDtoForList(
    Guid Id,
    string KeyPrefix,
    string Name,
    string[] Scopes,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public sealed record ApiKeyDtoForDetail(
    Guid Id,
    Guid OrganizationId,
    string KeyPrefix,
    string Name,
    string[] Scopes,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt
);

public sealed record ApiKeyDtoForCreate(
    string Name,
    string[] Scopes,
    DateTimeOffset? ExpiresAt
);

public sealed record ApiKeyDtoForCreated(
    Guid Id,
    string KeyPrefix,
    string PlainTextKey
);

public sealed record ApiKeyValidationResult(
    bool IsValid,
    Guid? OrganizationId,
    string[] Scopes,
    string? ErrorMessage
);
