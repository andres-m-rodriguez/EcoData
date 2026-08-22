using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.DataAccess.Repositories;

public sealed class OrganizationRoleRepository(
    IDbContextFactory<OrganizationDbContext> contextFactory
) : IOrganizationRoleRepository
{
    public async Task<IReadOnlyList<OrganizationRoleDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Creation order keeps the defaults in Owner, Admin, Contributor, Viewer order.
        var roles = await context
            .OrganizationRoles.Where(r => r.OrganizationId == organizationId)
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                Permissions = r.Permissions.Select(p => p.Permission).OrderBy(p => p).ToList(),
                MemberCount = r.Members.Count,
            })
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => new OrganizationRoleDto(r.Id, r.Name, r.Permissions, r.MemberCount))
            .ToList();
    }

    public async Task<OrganizationRoleDto?> GetByIdAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var role = await context
            .OrganizationRoles.Where(r => r.OrganizationId == organizationId && r.Id == roleId)
            .Select(r => new
            {
                r.Id,
                r.Name,
                Permissions = r.Permissions.Select(p => p.Permission).OrderBy(p => p).ToList(),
                MemberCount = r.Members.Count,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        return new OrganizationRoleDto(role.Id, role.Name, role.Permissions, role.MemberCount);
    }

    public async Task<OrganizationRoleDto?> GetByNameAsync(
        Guid organizationId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var role = await context
            .OrganizationRoles.Where(r => r.OrganizationId == organizationId && r.Name == name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                Permissions = r.Permissions.Select(p => p.Permission).OrderBy(p => p).ToList(),
                MemberCount = r.Members.Count,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        return new OrganizationRoleDto(role.Id, role.Name, role.Permissions, role.MemberCount);
    }

    public async Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludeRoleId = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationRoles.AnyAsync(
            r =>
                r.OrganizationId == organizationId
                && r.Name == name
                && (excludeRoleId == null || r.Id != excludeRoleId),
            cancellationToken
        );
    }

    public async Task<OrganizationRoleDto> CreateAsync(
        Guid organizationId,
        string name,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new OrganizationRole
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var permission in permissions)
        {
            entity.Permissions.Add(
                new OrganizationRolePermission { RoleId = entity.Id, Permission = permission }
            );
        }

        context.OrganizationRoles.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new OrganizationRoleDto(
            entity.Id,
            entity.Name,
            permissions.Order().ToList(),
            MemberCount: 0
        );
    }

    public async Task<OrganizationRoleDto?> UpdateAsync(
        Guid organizationId,
        Guid roleId,
        string name,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context
            .OrganizationRoles.AsTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId && r.Id == roleId,
                cancellationToken
            );

        if (entity is null)
        {
            return null;
        }

        entity.Name = name;

        var wanted = permissions.ToHashSet(StringComparer.Ordinal);

        foreach (var existing in entity.Permissions.ToList())
        {
            if (!wanted.Remove(existing.Permission))
            {
                context.OrganizationRolePermissions.Remove(existing);
            }
        }

        foreach (var added in wanted)
        {
            entity.Permissions.Add(
                new OrganizationRolePermission { RoleId = entity.Id, Permission = added }
            );
        }

        await context.SaveChangesAsync(cancellationToken);

        var memberCount = await context.OrganizationMembers.CountAsync(
            m => m.RoleId == roleId,
            cancellationToken
        );

        return new OrganizationRoleDto(
            entity.Id,
            entity.Name,
            permissions.Order().ToList(),
            memberCount
        );
    }

    public async Task<bool> IsInUseAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await context.OrganizationMembers.AnyAsync(m => m.RoleId == roleId, cancellationToken))
        {
            return true;
        }

        return await context.OrganizationAccessRequests.AnyAsync(
            r => r.RoleId == roleId && r.Status == OrganizationAccessRequestStatus.Pending,
            cancellationToken
        );
    }

    public async Task<bool> DeleteAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context
            .OrganizationRoles.AsTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId && r.Id == roleId,
                cancellationToken
            );

        if (entity is null)
        {
            return false;
        }

        // Resolved access requests keep their FK; the role row is what they pointed at, so
        // those references are cleared rather than blocking the delete.
        await context
            .OrganizationAccessRequests.Where(r => r.RoleId == roleId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.RoleId, (Guid?)null),
                cancellationToken
            );

        context.OrganizationRoles.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
