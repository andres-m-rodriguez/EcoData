using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class SensorRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : ISensorRepository
{
    public async Task<bool> ExistsAsync(string externalId, Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .AnyAsync(s => s.ExternalId == externalId && s.SourceId == dataSourceId, cancellationToken);
    }

    public async Task<Sensor?> GetByExternalIdAsync(string externalId, Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .FirstOrDefaultAsync(s => s.ExternalId == externalId && s.SourceId == dataSourceId, cancellationToken);
    }

    public async Task<SensorForDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.Id == id)
            .Select(s => new SensorForDetailDto(
                s.Id,
                s.SourceId,
                s.ExternalId,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.Municipality,
                s.IsActive,
                s.CreatedAt,
                s.DataSource!.Name
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorForListDto>> GetByDataSourceAsync(Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.SourceId == dataSourceId)
            .Select(s => new SensorForListDto(
                s.Id,
                s.ExternalId,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.Municipality,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorForListDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.IsActive)
            .Select(s => new SensorForListDto(
                s.Id,
                s.ExternalId,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.Municipality,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Sensor>> GetSensorsByExternalIdsAsync(
        Guid dataSourceId,
        IEnumerable<string> externalIds,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var idList = externalIds.ToList();
        var sensors = await context.Sensors
            .Where(s => s.SourceId == dataSourceId && idList.Contains(s.ExternalId))
            .ToListAsync(cancellationToken);

        return sensors.ToDictionary(s => s.ExternalId);
    }

    public async Task CreateManyAsync(IEnumerable<Sensor> sensors, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Sensors.AddRange(sensors);
        await context.SaveChangesAsync(cancellationToken);
    }
}
