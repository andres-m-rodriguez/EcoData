using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class ConservationLinkRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : IConservationLinkRepository
{
    public async Task<ConservationLinksDtoForSpecies> GetBySpeciesAsync(
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

    public async Task<IReadOnlyList<SpeciesConservationCodesDto>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .FwsLinks
            .Where(l => l.Species.MunicipalitySpecies.Any(ms => ms.MunicipalityId == municipalityId))
            .GroupBy(l => l.SpeciesId)
            .Select(g => new SpeciesConservationCodesDto(
                g.Key,
                g.Select(l => l.NrcsPractice.Code).Distinct().ToList(),
                g.Select(l => l.FwsAction.Code).Distinct().ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}
