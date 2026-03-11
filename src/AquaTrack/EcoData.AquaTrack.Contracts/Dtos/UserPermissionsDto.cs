namespace EcoData.AquaTrack.Contracts.Dtos;

public record UserPermissionsDto(
    Guid OrganizationId,
    IReadOnlyList<string> Permissions,
    bool IsGlobalAdmin
);
