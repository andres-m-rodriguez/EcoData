using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Database;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.DataAccess.Repositories;

public sealed class OrganizationMembershipRepository(IDbContextFactory<OrganizationDbContext> contextFactory)
    : IOrganizationMembershipRepository
{
    public async Task<IReadOnlyList<OrganizationMembershipDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationMembers
            .Where(m => m.UserId == userId)
            .Select(m => new OrganizationMembershipDto(
                m.OrganizationId,
                m.Role!.Name,
                m.Role.Permissions.Select(p => p.Permission).ToList()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationMembershipDto?> GetAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationMembers
            .Where(m => m.UserId == userId && m.OrganizationId == organizationId)
            .Select(m => new OrganizationMembershipDto(
                m.OrganizationId,
                m.Role!.Name,
                m.Role.Permissions.Select(p => p.Permission).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
