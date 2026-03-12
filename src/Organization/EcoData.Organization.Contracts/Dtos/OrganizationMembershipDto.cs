namespace EcoData.Organization.Contracts.Dtos;

public record OrganizationMembershipDto(
    Guid OrganizationId,
    string RoleName,
    IReadOnlyList<string> Permissions
);
