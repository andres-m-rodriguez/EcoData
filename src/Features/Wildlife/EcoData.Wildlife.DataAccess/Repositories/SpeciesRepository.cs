using System.Runtime.CompilerServices;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class SpeciesRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : ISpeciesRepository
{
    public async Task<SpeciesDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Species.Where(s => s.Id == id)
            .Select(s => new SpeciesDtoForDetail(
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                s.ElCode,
                s.GRank,
                s.SRank,
                s.ImageSourceUrl,
                s.ProfileImageData != null,
                s.CategoryLinks
                    .Select(cl => new SpeciesCategoryDtoForList(
                        cl.Category.Id,
                        cl.Category.Code,
                        cl.Category.Name
                    ))
                    .ToList(),
                s.MunicipalitySpecies.Select(ms => ms.MunicipalityId).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Species.AsQueryable();

        query = ApplyFilters(query, parameters);

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.Id < parameters.Cursor.Value);
        }

        await foreach (
            var species in query
                .OrderByDescending(s => s.Id)
                .Take(parameters.PageSize + 1)
                .Select(static s => new SpeciesDtoForList(
                    s.Id,
                    s.CommonName,
                    s.ScientificName,
                    s.IsFauna,
                    s.GRank,
                    s.SRank,
                    s.ProfileImageData != null
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return species;
        }
    }

    public async Task<int> GetCountAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Species.AsQueryable();
        query = ApplyFilters(query, parameters);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .MunicipalitySpecies.Where(ms => ms.MunicipalityId == municipalityId)
            .Select(ms => new SpeciesDtoForList(
                ms.Species.Id,
                ms.Species.CommonName,
                ms.Species.ScientificName,
                ms.Species.IsFauna,
                ms.Species.GRank,
                ms.Species.SRank,
                ms.Species.ProfileImageData != null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategoryLinks.Where(scl => scl.CategoryId == categoryId)
            .Select(scl => new SpeciesDtoForList(
                scl.Species.Id,
                scl.Species.CommonName,
                scl.Species.ScientificName,
                scl.Species.IsFauna,
                scl.Species.GRank,
                scl.Species.SRank,
                scl.Species.ProfileImageData != null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]?> GetProfileImageAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Species.Where(s => s.Id == id)
            .Select(s => s.ProfileImageData)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Database.Models.Species> ApplyFilters(
        IQueryable<Database.Models.Species> query,
        SpeciesParameters parameters
    )
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(s =>
                s.ScientificName.ToLower().Contains(search)
            );
        }

        if (parameters.IsFauna.HasValue)
        {
            query = query.Where(s => s.IsFauna == parameters.IsFauna.Value);
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(s =>
                s.CategoryLinks.Any(cl => cl.CategoryId == parameters.CategoryId.Value)
            );
        }

        if (parameters.MunicipalityId.HasValue)
        {
            query = query.Where(s =>
                s.MunicipalitySpecies.Any(ms => ms.MunicipalityId == parameters.MunicipalityId.Value)
            );
        }

        return query;
    }
}
