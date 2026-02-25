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

    public async Task<SensorDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.Id == id)
            .Select(s => new SensorDtoForDetail(
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

    public async Task<IReadOnlyList<SensorDtoForList>> GetByDataSourceAsync(Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.SourceId == dataSourceId)
            .Select(s => new SensorDtoForList(
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

    public async Task<IReadOnlyList<SensorDtoForList>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors
            .Where(s => s.IsActive)
            .Select(s => new SensorDtoForList(
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

    public async Task<Dictionary<string, SensorDtoForCreated>> GetSensorsByExternalIdsAsync(
        Guid dataSourceId,
        ICollection<string> externalIds,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var sensors = await context.Sensors
            .Where(s => s.SourceId == dataSourceId && externalIds.Contains(s.ExternalId))
            .Select(s => new SensorDtoForCreated(s.Id, s.ExternalId))
            .ToListAsync(cancellationToken);

        return sensors.ToDictionary(s => s.ExternalId);
    }

    public async Task<IReadOnlyList<SensorDtoForCreated>> CreateManyAsync(
        ICollection<SensorDtoForCreate> dtos,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entities = dtos.Select(dto => new Sensor
        {
            Id = Guid.CreateVersion7(),
            SourceId = dto.SourceId,
            ExternalId = dto.ExternalId,
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Municipality = dto.Municipality,
            IsActive = dto.IsActive,
            CreatedAt = now,
        }).ToList();

        context.Sensors.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        return entities.Select(e => new SensorDtoForCreated(e.Id, e.ExternalId)).ToList();
    }
}
