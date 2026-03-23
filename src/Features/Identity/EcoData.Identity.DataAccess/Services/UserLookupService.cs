using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Dtos;
using EcoData.Identity.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Identity.DataAccess.Services;

public sealed class UserLookupService(IDbContextFactory<IdentityDbContext> contextFactory)
    : IUserLookupService
{
    public async Task<IReadOnlyDictionary<Guid, UserLookupDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserLookupDto>();
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var users = await context
            .Users.Where(u => ids.Contains(u.Id))
            .Select(u => new UserLookupDto(u.Id, u.Email!, u.DisplayName))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id);
    }

    public async Task<UserLookupDto?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Users.Where(u => u.Id == userId)
            .Select(u => new UserLookupDto(u.Id, u.Email!, u.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> IsGlobalAdminAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Users.AnyAsync(
            u => u.Id == userId && u.GlobalRole == Database.Models.GlobalRole.GlobalAdmin,
            cancellationToken
        );
    }
}
