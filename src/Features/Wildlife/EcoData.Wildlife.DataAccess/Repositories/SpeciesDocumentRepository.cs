using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class SpeciesDocumentRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : ISpeciesDocumentRepository
{
    public async Task<IReadOnlyList<SpeciesDocumentDto>> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesDocuments
            .Where(d => d.SpeciesId == speciesId)
            .OrderBy(d => d.FileName)
            .Select(d => new SpeciesDocumentDto(d.Id, d.Title, d.FileName))
            .ToListAsync(cancellationToken);
    }
}
