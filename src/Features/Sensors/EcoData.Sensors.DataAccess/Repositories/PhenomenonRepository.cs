using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Database;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class PhenomenonRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IPhenomenonRepository
{
    public async Task<IReadOnlyList<PhenomenonDtoForList>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Phenomena.OrderBy(p => p.Name)
            .Select(p => new PhenomenonDtoForList(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.CanonicalUnit,
                p.DefaultValueShape.ToString(),
                p.Capabilities
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhenomenonDtoForList>> GetByCapabilityAsync(
        string capability,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Phenomena.Where(p => p.Capabilities.Contains(capability))
            .OrderBy(p => p.Name)
            .Select(p => new PhenomenonDtoForList(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.CanonicalUnit,
                p.DefaultValueShape.ToString(),
                p.Capabilities
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<PhenomenonDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Phenomena.Where(p => p.Id == id)
            .Select(p => new PhenomenonDtoForDetail(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.CanonicalUnit,
                p.DefaultValueShape.ToString(),
                p.Capabilities,
                p.CreatedAt,
                p.Parameters.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PhenomenonDtoForDetail?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Phenomena.Where(p => p.Code == code)
            .Select(p => new PhenomenonDtoForDetail(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.CanonicalUnit,
                p.DefaultValueShape.ToString(),
                p.Capabilities,
                p.CreatedAt,
                p.Parameters.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
