namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record OrganizationBlockedUserDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string? Reason,
    Guid BlockedByUserId,
    string BlockedByDisplayName,
    DateTimeOffset BlockedAt
);

public sealed record BlockUserRequest(Guid UserId, string? Reason);
