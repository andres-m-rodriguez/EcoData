using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class ReadingRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IReadingRepository
{
    public async Task<IReadOnlyList<ReadingDtoForDetail>> GetBySensorAsync(
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
            var fromUtc = from.Value.ToUniversalTime();
            query = query.Where(r => r.RecordedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.ToUniversalTime();
            query = query.Where(r => r.RecordedAt <= toUtc);
        }

        return await query
            .OrderByDescending(r => r.RecordedAt)
            .Take(limit)
            .Select(r => new ReadingDtoForDetail(
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

    public async Task<IReadOnlyList<ReadingDtoForList>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Readings
            .OrderByDescending(r => r.RecordedAt)
            .Take(limit)
            .Select(r => new ReadingDtoForList(
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

    public async Task CreateManyAsync(ICollection<ReadingDtoForCreate> dtos, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entities = dtos.Select(dto => new Reading
        {
            Id = Guid.CreateVersion7(),
            SensorId = dto.SensorId,
            Parameter = dto.Parameter,
            Value = dto.Value,
            Unit = dto.Unit,
            RecordedAt = dto.RecordedAt.ToUniversalTime(),
            IngestedAt = now,
        });

        context.Readings.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);
    }
}
