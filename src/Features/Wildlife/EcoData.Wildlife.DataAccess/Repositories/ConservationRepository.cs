using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class ConservationRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : IConservationRepository
{
    public async Task<IReadOnlyList<FwsActionDtoForList>> GetAllFwsActionsAsync(
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

    public async Task<IReadOnlyList<NrcsPracticeDtoForList>> GetAllNrcsPracticesAsync(
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

    public async Task<ConservationLinksDtoForSpecies> GetLinksForSpeciesAsync(
        Guid speciesId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var links = await context
            .FwsLinks
            .Where(l => l.SpeciesId == speciesId)
            .Select(l => new FwsLinkDtoForDetail(
                l.Id,
                l.SpeciesId,
                new FwsActionDtoForList(l.FwsAction.Id, l.FwsAction.Code, l.FwsAction.Name),
                new NrcsPracticeDtoForList(l.NrcsPractice.Id, l.NrcsPractice.Code, l.NrcsPractice.Name),
                l.Justification
            ))
            .ToListAsync(cancellationToken);

        return new ConservationLinksDtoForSpecies(links);
    }
}
