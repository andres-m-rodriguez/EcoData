using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class ReadingRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IReadingRepository
{
    public async Task<IReadOnlyList<ReadingDto>> GetBySensorAsync(
        Guid sensorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Readings.Where(r => r.SensorId == sensorId);

        if (from.HasValue)
        {
            query = query.Where(r => r.RecordedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.RecordedAt <= to.Value);
        }

        return await query
            .OrderByDescending(r => r.RecordedAt)
            .Take(limit)
            .Select(r => new ReadingDto(
                r.Id,
                r.SensorId,
                r.Parameter,
                r.Value,
                r.Unit,
                r.RecordedAt,
                r.IngestedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReadingWithSensorDto>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Readings
            .OrderByDescending(r => r.RecordedAt)
            .Take(limit)
            .Select(r => new ReadingWithSensorDto(
                r.Id,
                r.SensorId,
                r.Sensor!.Name,
                r.Parameter,
                r.Value,
                r.Unit,
                r.RecordedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<(Guid SensorId, string Parameter, DateTimeOffset RecordedAt)>> GetExistingKeysAsync(
        IEnumerable<Guid> sensorIds,
        DateTimeOffset minRecordedAt,
        DateTimeOffset maxRecordedAt,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var idList = sensorIds.ToList();

        var existingKeys = await context.Readings
            .Where(r => idList.Contains(r.SensorId) &&
                        r.RecordedAt >= minRecordedAt &&
                        r.RecordedAt <= maxRecordedAt)
            .Select(r => new { r.SensorId, r.Parameter, r.RecordedAt })
            .ToListAsync(cancellationToken);

        return existingKeys
            .Select(k => (k.SensorId, k.Parameter, k.RecordedAt))
            .ToHashSet();
    }

    public async Task CreateManyAsync(IEnumerable<Reading> readings, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Readings.AddRange(readings);
        await context.SaveChangesAsync(cancellationToken);
    }
}
