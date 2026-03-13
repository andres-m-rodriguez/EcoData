namespace EcoData.Organization.Contracts.Dtos;

public record OrganizationPermissionsDto(string? Role, IReadOnlyList<string> Permissions);
