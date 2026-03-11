namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationMemberRepository
{
    Task<IReadOnlyList<OrganizationMembershipDto>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMembershipDto?> GetByUserAndOrganizationAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}

public record OrganizationMembershipDto(
    Guid OrganizationId,
    string RoleName,
    IReadOnlyList<string> Permissions
);
