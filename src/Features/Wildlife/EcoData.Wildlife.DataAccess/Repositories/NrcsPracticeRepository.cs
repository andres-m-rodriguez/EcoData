using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class NrcsPracticeRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : INrcsPracticeRepository
{
    public async Task<IReadOnlyList<NrcsPracticeDtoForList>> GetListAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .NrcsPractices
            .OrderBy(p => p.Code)
            .Select(p => new NrcsPracticeDtoForList(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<NrcsPracticeDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .NrcsPractices
            .Where(p => p.Code == code)
            .Select(p => new NrcsPracticeDtoForDetail(p.Id, p.Code, p.Name, p.Description, p.NrcsUrl))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
