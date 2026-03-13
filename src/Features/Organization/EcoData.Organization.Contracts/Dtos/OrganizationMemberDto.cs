namespace EcoData.Organization.Contracts.Dtos;

public record OrganizationMemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleName,
    DateTimeOffset CreatedAt
);

public record UpdateMemberRoleRequest(string Role);
