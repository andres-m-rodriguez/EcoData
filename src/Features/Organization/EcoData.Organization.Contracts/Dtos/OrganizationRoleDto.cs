namespace EcoData.Organization.Contracts.Dtos;

public sealed record OrganizationRoleDto(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions,
    int MemberCount
);
