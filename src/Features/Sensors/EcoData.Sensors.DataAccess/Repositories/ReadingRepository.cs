using System.Runtime.CompilerServices;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class ReadingRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IReadingRepository
{
    public async Task<IReadOnlyList<ReadingDtoForDetail>> GetBySensorAsync(
        Guid sensorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default
    )
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

    public async IAsyncEnumerable<ReadingDtoForDetail> GetReadingsAsync(
        Guid sensorId,
        ReadingParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Readings.Where(r => r.SensorId == sensorId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.Parameter.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Parameter))
        {
            query = query.Where(r => r.Parameter == parameters.Parameter);
        }

        if (parameters.FromDate.HasValue)
        {
            var fromUtc = parameters.FromDate.Value.ToUniversalTime();
            query = query.Where(r => r.RecordedAt >= fromUtc);
        }

        if (parameters.ToDate.HasValue)
        {
            var toUtc = parameters.ToDate.Value.ToUniversalTime();
            query = query.Where(r => r.RecordedAt <= toUtc);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(r => r.Id < parameters.Cursor.Value);
        }

        await foreach (
            var reading in query
                .OrderByDescending(r => r.RecordedAt)
                .ThenByDescending(r => r.Id)
                .Take(parameters.PageSize + 1)
                .Select(static r => new ReadingDtoForDetail(
                    r.Id,
                    r.SensorId,
                    r.Parameter,
                    r.Value,
                    r.Unit,
                    r.RecordedAt,
                    r.IngestedAt
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return reading;
        }
    }

    public async Task<IReadOnlyList<string>> GetDistinctParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Readings.Where(r => r.SensorId == sensorId)
            .Select(r => r.Parameter)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReadingDtoForList>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Readings.OrderByDescending(r => r.RecordedAt)
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

    public async Task CreateManyAsync(
        ICollection<ReadingDtoForCreate> dtos,
        CancellationToken cancellationToken = default
    )
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
