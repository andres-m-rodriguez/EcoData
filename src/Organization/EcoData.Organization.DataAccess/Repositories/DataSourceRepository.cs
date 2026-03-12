using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.DataAccess.Repositories;

public sealed class DataSourceRepository(IDbContextFactory<OrganizationDbContext> contextFactory)
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
                ds.OrganizationId,
                ds.Name,
                ds.Type.ToString(),
                ds.BaseUrl,
                ds.PullIntervalSeconds,
                ds.IsActive,
                ds.CreatedAt,
                0 // SensorCount - Note: Sensors are not tracked in Organization module
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataSourceDtoForList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataSources
            .Select(ds => new DataSourceDtoForList(
                ds.Id,
                ds.OrganizationId,
                ds.Name,
                ds.Type.ToString(),
                ds.BaseUrl,
                ds.PullIntervalSeconds,
                ds.IsActive,
                ds.CreatedAt,
                0 // SensorCount - Note: Sensors are not tracked in Organization module
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<DataSourceDtoForCreated> CreateAsync(DataSourceDtoForCreate dto, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new DataSource
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = dto.OrganizationId,
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
