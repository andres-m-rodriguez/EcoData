namespace EcoData.Identity.Contracts.Responses;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTimeOffset CreatedAt
);
