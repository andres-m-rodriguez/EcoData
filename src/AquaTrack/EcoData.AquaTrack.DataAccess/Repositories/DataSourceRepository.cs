using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class DataSourceRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IDataSourceRepository
{
    public async Task<DataSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .FirstOrDefaultAsync(ds => ds.Name == name, cancellationToken);
    }

    public async Task<DataSourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Where(ds => ds.Id == id)
            .Select(ds => new DataSourceDto(
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

    public async Task<IReadOnlyList<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Select(ds => new DataSourceDto(
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

    public async Task<DataSource> CreateAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.DataSources.Add(dataSource);
        await context.SaveChangesAsync(cancellationToken);
        return dataSource;
    }
}
