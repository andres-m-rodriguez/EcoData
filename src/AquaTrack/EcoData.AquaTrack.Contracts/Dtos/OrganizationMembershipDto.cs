namespace EcoData.AquaTrack.Contracts.Dtos;

public record OrganizationMembershipDto(
    Guid OrganizationId,
    string RoleName,
    IReadOnlyList<string> Permissions
);
