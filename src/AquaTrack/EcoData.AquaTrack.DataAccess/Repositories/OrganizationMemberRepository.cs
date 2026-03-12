using System.Runtime.CompilerServices;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class OrganizationMemberRepository(
    IDbContextFactory<AquaTrackDbContext> contextFactory,
    IUserLookupRepository userLookupRepository
) : IOrganizationMemberRepository
{
    public async IAsyncEnumerable<OrganizationMemberDto> GetAllAsync(
        Guid organizationId,
        OrganizationMemberParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.OrganizationMembers.Where(m => m.OrganizationId == organizationId);

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(m => m.Id.CompareTo(parameters.Cursor.Value) > 0);
        }

        var members = await query
            .OrderBy(m => m.Id)
            .Take(parameters.PageSize)
            .Select(m => new
            {
                m.Id,
                m.UserId,
                RoleName = m.Role!.Name,
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        if (members.Count == 0)
        {
            yield break;
        }

        var userIds = members.Select(m => m.UserId).Distinct();
        var users = await userLookupRepository.GetByIdsAsync(userIds, cancellationToken);

        foreach (var member in members)
        {
            var user = users.GetValueOrDefault(member.UserId);
            yield return new OrganizationMemberDto(
                member.Id,
                member.UserId,
                user?.Email ?? "",
                user?.DisplayName ?? "",
                member.RoleName,
                member.CreatedAt
            );
        }
    }

    public async Task<OrganizationMemberDto?> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var member = await context
            .OrganizationMembers.Where(m =>
                m.OrganizationId == organizationId && m.UserId == userId
            )
            .Select(m => new
            {
                m.Id,
                m.UserId,
                RoleName = m.Role!.Name,
                m.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return null;
        }

        var user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);

        return new OrganizationMemberDto(
            member.Id,
            member.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            member.RoleName,
            member.CreatedAt
        );
    }

    public async Task<OrganizationMemberDto?> CreateAsync(
        Guid organizationId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var role = await context
            .OrganizationRoles.Where(r =>
                r.Name == roleName
                && (r.OrganizationId == organizationId || r.OrganizationId == null)
            )
            .OrderByDescending(r => r.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserId = userId,
            RoleId = role.Id,
            CreatedAt = now,
        };

        context.OrganizationMembers.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        var user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);

        return new OrganizationMemberDto(
            entity.Id,
            entity.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            role.Name,
            entity.CreatedAt
        );
    }

    public async Task<OrganizationMemberDto?> UpdateAsync(
        Guid organizationId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var member = await context
            .OrganizationMembers.AsTracking()
            .FirstOrDefaultAsync(
                m => m.OrganizationId == organizationId && m.UserId == userId,
                cancellationToken
            );

        if (member is null)
        {
            return null;
        }

        var role = await context
            .OrganizationRoles.Where(r =>
                r.Name == roleName
                && (r.OrganizationId == organizationId || r.OrganizationId == null)
            )
            .OrderByDescending(r => r.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        member.RoleId = role.Id;
        await context.SaveChangesAsync(cancellationToken);

        var user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);

        return new OrganizationMemberDto(
            member.Id,
            member.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            role.Name,
            member.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var member = await context
            .OrganizationMembers.AsTracking()
            .FirstOrDefaultAsync(
                m => m.OrganizationId == organizationId && m.UserId == userId,
                cancellationToken
            );

        if (member is null)
        {
            return false;
        }

        context.OrganizationMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExistsAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationMembers.AnyAsync(
            m => m.OrganizationId == organizationId && m.UserId == userId,
            cancellationToken
        );
    }
}
