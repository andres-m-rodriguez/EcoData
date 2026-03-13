using System.Runtime.CompilerServices;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.Database;
using EcoData.Locations.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Locations.DataAccess.Repositories;

public sealed class StateRepository(IDbContextFactory<LocationsDbContext> contextFactory)
    : IStateRepository
{
    public IAsyncEnumerable<StateDtoForList> GetStatesAsync(
        StateParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetStatesInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<StateDtoForList> GetStatesInternalAsync(
        StateParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.States.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search)
                || s.Code.ToLower().Contains(search)
            );
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.Id > parameters.Cursor.Value);
        }

        await foreach (var state in query
            .OrderBy(s => s.Id)
            .Take(parameters.PageSize + 1)
            .Select(static s => new StateDtoForList(
                s.Id,
                s.Name,
                s.Code,
                s.FipsCode
            ))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async Task<StateDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.States
            .Where(s => s.Id == id)
            .Select(s => new StateDtoForDetail(
                s.Id,
                s.Name,
                s.Code,
                s.FipsCode,
                s.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StateDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedCode = code.ToUpperInvariant();

        return await context.States
            .Where(s => s.Code == normalizedCode)
            .Select(s => new StateDtoForDetail(
                s.Id,
                s.Name,
                s.Code,
                s.FipsCode,
                s.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetIdByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedCode = code.ToUpperInvariant();

        return await context.States
            .Where(s => s.Code == normalizedCode)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
