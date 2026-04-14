namespace EcoData.Identity.Contracts.Responses;

public sealed record LoginResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserInfo User
);
