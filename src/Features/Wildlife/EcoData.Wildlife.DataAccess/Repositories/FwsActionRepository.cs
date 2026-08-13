using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class FwsActionRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : IFwsActionRepository
{
    public async Task<IReadOnlyList<FwsActionDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .FwsActions
            .OrderBy(a => a.Code)
            .Select(a => new FwsActionDtoForList(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);
    }
}
