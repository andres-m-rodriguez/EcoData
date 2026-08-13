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

    public async Task<IReadOnlyList<PracticeActionDtoForList>> GetActionsByPracticeAsync(
        string practiceCode,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Queried from the action side so the practice filter stays a subquery:
        // a link row exists per (practice, action, species), so the species tally
        // has to de-duplicate rather than count links.
        return await context
            .FwsActions
            .Where(a => a.FwsLinks.Any(l => l.NrcsPractice.Code == practiceCode))
            .OrderBy(a => a.Code)
            .Select(a => new PracticeActionDtoForList(
                new FwsActionDtoForList(a.Id, a.Code, a.Name),
                a.FwsLinks
                    .Where(l => l.NrcsPractice.Code == practiceCode)
                    .Select(l => l.SpeciesId)
                    .Distinct()
                    .Count()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActionPracticeDtoForList>> GetPracticesByActionAsync(
        string actionCode,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Queried from the practice side so the action filter stays a subquery:
        // a link row exists per (practice, action, species), so the species tally
        // has to de-duplicate rather than count links.
        return await context
            .NrcsPractices
            .Where(p => p.FwsLinks.Any(l => l.FwsAction.Code == actionCode))
            .OrderBy(p => p.Code)
            .Select(p => new ActionPracticeDtoForList(
                new NrcsPracticeDtoForList(p.Id, p.Code, p.Name),
                p.FwsLinks
                    .Where(l => l.FwsAction.Code == actionCode)
                    .Select(l => l.SpeciesId)
                    .Distinct()
                    .Count()
            ))
            .ToListAsync(cancellationToken);
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
