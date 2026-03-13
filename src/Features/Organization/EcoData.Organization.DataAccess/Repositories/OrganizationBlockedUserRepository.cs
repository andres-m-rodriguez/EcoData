using System.Runtime.CompilerServices;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.DataAccess.Repositories;

public sealed class OrganizationBlockedUserRepository(
    IDbContextFactory<OrganizationDbContext> contextFactory,
    IUserLookupRepository userLookupRepository
) : IOrganizationBlockedUserRepository
{
    public async IAsyncEnumerable<OrganizationBlockedUserDto> GetByOrganizationAsync(
        Guid organizationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var blockedUsers = await context
            .OrganizationBlockedUsers.Where(b => b.OrganizationId == organizationId)
            .OrderByDescending(b => b.BlockedAt)
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.Reason,
                b.BlockedByUserId,
                b.BlockedAt,
            })
            .ToListAsync(cancellationToken);

        if (blockedUsers.Count == 0)
        {
            yield break;
        }

        var userIds = blockedUsers
            .SelectMany(b => new[] { b.UserId, b.BlockedByUserId })
            .Distinct();

        var users = await userLookupRepository.GetByIdsAsync(userIds, cancellationToken);

        foreach (var blocked in blockedUsers)
        {
            var user = users.GetValueOrDefault(blocked.UserId);
            var blockedBy = users.GetValueOrDefault(blocked.BlockedByUserId);

            yield return new OrganizationBlockedUserDto(
                blocked.Id,
                blocked.UserId,
                user?.DisplayName ?? "Unknown User",
                blocked.Reason,
                blocked.BlockedByUserId,
                blockedBy?.DisplayName ?? "Unknown",
                blocked.BlockedAt
            );
        }
    }

    public async Task<bool> IsBlockedAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationBlockedUsers.AnyAsync(
            b => b.OrganizationId == organizationId && b.UserId == userId,
            cancellationToken
        );
    }

    public async Task<OrganizationBlockedUserDto> BlockAsync(
        Guid organizationId,
        Guid userId,
        Guid blockedByUserId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new OrganizationBlockedUser
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserId = userId,
            BlockedByUserId = blockedByUserId,
            Reason = reason,
            BlockedAt = DateTimeOffset.UtcNow,
        };

        context.OrganizationBlockedUsers.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        var users = await userLookupRepository.GetByIdsAsync(
            [userId, blockedByUserId],
            cancellationToken
        );

        var user = users.GetValueOrDefault(userId);
        var blockedBy = users.GetValueOrDefault(blockedByUserId);

        return new OrganizationBlockedUserDto(
            entity.Id,
            entity.UserId,
            user?.DisplayName ?? "Unknown User",
            entity.Reason,
            entity.BlockedByUserId,
            blockedBy?.DisplayName ?? "Unknown",
            entity.BlockedAt
        );
    }

    public async Task<bool> UnblockAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context
            .OrganizationBlockedUsers.AsTracking()
            .FirstOrDefaultAsync(
                b => b.OrganizationId == organizationId && b.UserId == userId,
                cancellationToken
            );

        if (entity is null)
        {
            return false;
        }

        context.OrganizationBlockedUsers.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
