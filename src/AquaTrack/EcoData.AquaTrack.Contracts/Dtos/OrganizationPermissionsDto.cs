namespace EcoData.AquaTrack.Contracts.Dtos;

public record OrganizationPermissionsDto(string? Role, IReadOnlyList<string> Permissions);
