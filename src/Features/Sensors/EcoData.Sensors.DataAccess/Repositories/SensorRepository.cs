using System.Runtime.CompilerServices;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class SensorRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISensorRepository
{
    public async Task<bool> ExistsAsync(
        string externalId,
        Guid dataSourceId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors.AnyAsync(
            s => s.ExternalId == externalId && s.SourceId == dataSourceId,
            cancellationToken
        );
    }

    public async Task<SensorDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Sensors.Where(s => s.Id == id)
            .Select(s => new SensorDtoForDetail(
                s.Id,
                s.OrganizationId,
                s.SourceId,
                s.ExternalId,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.MunicipalityId,
                s.IsActive,
                s.CreatedAt,
                null
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorDtoForList>> GetByDataSourceAsync(
        Guid dataSourceId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .Sensors.Where(s => s.SourceId == dataSourceId)
            .Select(s => new SensorDtoForList(
                s.Id,
                s.OrganizationId,
                s.SourceId,
                s.ExternalId,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.MunicipalityId,
                s.IsActive,
                null
            ))
            .ToListAsync(cancellationToken);
    }

    public async IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Sensors.AsQueryable();

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == parameters.IsActive.Value);
        }

        if (parameters.DataSourceId.HasValue)
        {
            query = query.Where(s => s.SourceId == parameters.DataSourceId.Value);
        }

        if (parameters.OrganizationId.HasValue)
        {
            query = query.Where(s => s.OrganizationId == parameters.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search));
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.Id > parameters.Cursor.Value);
        }

        await foreach (
            var sensor in query
                .OrderBy(s => s.Id)
                .Take(parameters.PageSize + 1)
                .Select(static s => new SensorDtoForList(
                    s.Id,
                    s.OrganizationId,
                    s.SourceId,
                    s.ExternalId,
                    s.Name,
                    s.Latitude,
                    s.Longitude,
                    s.MunicipalityId,
                    s.IsActive,
                    null
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return sensor;
        }
    }

    public async Task<int> GetSensorCountAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Sensors.AsQueryable();

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == parameters.IsActive.Value);
        }

        if (parameters.DataSourceId.HasValue)
        {
            query = query.Where(s => s.SourceId == parameters.DataSourceId.Value);
        }

        if (parameters.OrganizationId.HasValue)
        {
            query = query.Where(s => s.OrganizationId == parameters.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search));
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.Id > parameters.Cursor.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, SensorDtoForCreated>> GetSensorsByExternalIdsAsync(
        Guid dataSourceId,
        ICollection<string> externalIds,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var sensors = await context
            .Sensors.Where(s => s.SourceId == dataSourceId && externalIds.Contains(s.ExternalId))
            .Select(s => new SensorDtoForCreated(s.Id, s.ExternalId))
            .ToListAsync(cancellationToken);

        return sensors.ToDictionary(s => s.ExternalId);
    }

    public async Task<IReadOnlyList<SensorDtoForCreated>> CreateManyAsync(
        Guid organizationId,
        ICollection<SensorDtoForCreate> dtos,
        CancellationToken cancellationToken = default
    )
    {
        if (dtos.Count == 0)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entities = dtos.Select(dto => new Sensor
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = organizationId,
                SourceId = dto.SourceId,
                ExternalId = dto.ExternalId,
                Name = dto.Name,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                MunicipalityId = dto.MunicipalityId,
                IsActive = dto.IsActive,
                ReportingMode = ReportingMode.Pull,
                SensorTypeId = null,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        context.Sensors.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        return entities.Select(e => new SensorDtoForCreated(e.Id, e.ExternalId)).ToList();
    }

    public async Task<SensorDtoForDetail?> UpdateAsync(
        Guid id,
        SensorDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Sensors.AsTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.ExternalId = dto.ExternalId;
        entity.Name = dto.Name;
        entity.Latitude = dto.Latitude;
        entity.Longitude = dto.Longitude;
        entity.MunicipalityId = dto.MunicipalityId;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return new SensorDtoForDetail(
            entity.Id,
            entity.OrganizationId,
            entity.SourceId,
            entity.ExternalId,
            entity.Name,
            entity.Latitude,
            entity.Longitude,
            entity.MunicipalityId,
            entity.IsActive,
            entity.CreatedAt,
            null
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Sensors.AsTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        context.Sensors.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<int> GetCountByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sensors.CountAsync(
            s => s.OrganizationId == organizationId,
            cancellationToken
        );
    }

    public async IAsyncEnumerable<SensorDtoForList> GetByOrganizationAsync(
        Guid organizationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        await foreach (
            var sensor in context
                .Sensors.Where(s => s.OrganizationId == organizationId)
                .OrderBy(s => s.Name)
                .Select(static s => new SensorDtoForList(
                    s.Id,
                    s.OrganizationId,
                    s.SourceId,
                    s.ExternalId,
                    s.Name,
                    s.Latitude,
                    s.Longitude,
                    s.MunicipalityId,
                    s.IsActive,
                    null
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return sensor;
        }
    }
}
