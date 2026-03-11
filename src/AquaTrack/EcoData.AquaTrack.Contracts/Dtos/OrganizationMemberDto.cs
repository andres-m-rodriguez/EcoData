namespace EcoData.AquaTrack.Contracts.Dtos;

public record OrganizationMemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleName,
    DateTimeOffset CreatedAt
);

public record AddMemberRequest(Guid UserId, string Role);

public record UpdateMemberRoleRequest(string Role);
