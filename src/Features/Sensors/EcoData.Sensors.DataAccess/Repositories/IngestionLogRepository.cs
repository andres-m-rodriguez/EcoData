using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class IngestionLogRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IIngestionLogRepository
{
    public async Task<IngestionLogDtoForDetail?> GetLatestAsync(Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.IngestionLogs
            .Where(l => l.DataSourceId == dataSourceId)
            .OrderByDescending(l => l.IngestedAt)
            .Select(l => new IngestionLogDtoForDetail(
                l.Id,
                l.DataSourceId,
                l.IngestedAt,
                l.RecordCount,
                l.LastRecordedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(IngestionLogDtoForCreate dto, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new IngestionLog
        {
            Id = Guid.CreateVersion7(),
            DataSourceId = dto.DataSourceId,
            IngestedAt = DateTimeOffset.UtcNow,
            RecordCount = dto.RecordCount,
            LastRecordedAt = dto.LastRecordedAt.ToUniversalTime(),
        };

        context.IngestionLogs.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
