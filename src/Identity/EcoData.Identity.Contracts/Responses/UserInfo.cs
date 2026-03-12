using EcoData.Identity.Contracts.Authorization;

namespace EcoData.Identity.Contracts.Responses;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    GlobalRole? GlobalRole,
    DateTimeOffset CreatedAt
);
