using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class IngestionLogRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IIngestionLogRepository
{
    public async Task<IngestionLogDto?> GetLatestAsync(Guid dataSourceId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.IngestionLogs
            .Where(l => l.DataSourceId == dataSourceId)
            .OrderByDescending(l => l.IngestedAt)
            .Select(l => new IngestionLogDto(
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
