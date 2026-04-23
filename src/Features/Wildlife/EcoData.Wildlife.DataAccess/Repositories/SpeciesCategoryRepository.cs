using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class SpeciesCategoryRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : ISpeciesCategoryRepository
{
    public async Task<IReadOnlyList<SpeciesCategoryDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategories
            .OrderBy(c => c.Code)
            .Select(c => new SpeciesCategoryDtoForList(c.Id, c.Code, c.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<SpeciesCategoryDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategories
            .Where(c => c.Id == id)
            .Select(c => new SpeciesCategoryDtoForDetail(
                c.Id,
                c.Code,
                c.Name,
                c.SpeciesLinks.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SpeciesCategoryDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategories
            .Where(c => c.Code == code)
            .Select(c => new SpeciesCategoryDtoForDetail(
                c.Id,
                c.Code,
                c.Name,
                c.SpeciesLinks.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxonFacetDto>> GetCountsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategories
            .OrderBy(c => c.Code)
            .Select(c => new TaxonFacetDto(c.Code, c.SpeciesLinks.Count))
            .ToListAsync(cancellationToken);
    }
}
