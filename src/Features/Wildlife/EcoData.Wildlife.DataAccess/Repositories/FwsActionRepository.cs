using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class FwsActionRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : IFwsActionRepository
{
    public async Task<IReadOnlyList<FwsActionDtoForList>> GetListAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .FwsActions
            // Codes are "major.minor" and sort as numbers: 2.1 before 10.1, 2.9 before 2.10.
            .OrderBy(a => a.Code.IndexOf("."))
            .ThenBy(a => a.Code.Length)
            .ThenBy(a => a.Code)
            .Select(a => new FwsActionDtoForList(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);
    }
}
