using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class DataSourceRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IDataSourceRepository
{
    public async Task<DataSourceDtoForCreated?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Where(ds => ds.Name == name)
            .Select(ds => new DataSourceDtoForCreated(ds.Id, ds.Name, ds.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DataSourceDtoForList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Where(ds => ds.Id == id)
            .Select(ds => new DataSourceDtoForList(
                ds.Id,
                ds.Name,
                ds.Type.ToString(),
                ds.BaseUrl,
                ds.PullIntervalSeconds,
                ds.IsActive,
                ds.CreatedAt,
                ds.Sensors.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataSourceDtoForList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Select(ds => new DataSourceDtoForList(
                ds.Id,
                ds.Name,
                ds.Type.ToString(),
                ds.BaseUrl,
                ds.PullIntervalSeconds,
                ds.IsActive,
                ds.CreatedAt,
                ds.Sensors.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<DataSourceDtoForCreated> CreateAsync(DataSourceDtoForCreate dto, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new DataSource
        {
            Id = Guid.CreateVersion7(),
            Name = dto.Name,
            Type = Enum.Parse<DataSourceType>(dto.Type),
            BaseUrl = dto.BaseUrl,
            ApiKey = dto.ApiKey,
            PullIntervalSeconds = dto.PullIntervalSeconds,
            IsActive = dto.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.DataSources.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new DataSourceDtoForCreated(entity.Id, entity.Name, entity.CreatedAt);
    }
}
