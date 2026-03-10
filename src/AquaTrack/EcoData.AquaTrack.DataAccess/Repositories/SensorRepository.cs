using System.Runtime.CompilerServices;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class SensorRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
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
                s.DataSource != null ? s.DataSource.Name : null
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
                s.DataSource != null ? s.DataSource.Name : null
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
                    s.DataSource != null ? s.DataSource.Name : null
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return sensor;
        }
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

        // Get unique source IDs and fetch their organization IDs
        var sourceIds = dtos.Where(d => d.SourceId.HasValue)
            .Select(d => d.SourceId!.Value)
            .Distinct()
            .ToList();
        var sourceToOrg = await context
            .DataSources.Where(ds => sourceIds.Contains(ds.Id))
            .ToDictionaryAsync(ds => ds.Id, ds => ds.OrganizationId, cancellationToken);

        var entities = dtos.Select(dto =>
            {
                if (
                    !dto.SourceId.HasValue
                    || !sourceToOrg.TryGetValue(dto.SourceId.Value, out var organizationId)
                )
                {
                    throw new InvalidOperationException($"DataSource {dto.SourceId} not found");
                }

                return new Sensor
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
                };
            })
            .ToList();

        context.Sensors.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        return entities.Select(e => new SensorDtoForCreated(e.Id, e.ExternalId)).ToList();
    }

    public async Task<SensorDtoForCreated> CreateForOrganizationAsync(
        Guid organizationId,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entity = new Sensor
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            SourceId = null,
            ExternalId = dto.ExternalId,
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            MunicipalityId = dto.MunicipalityId,
            IsActive = dto.IsActive,
            ReportingMode = ReportingMode.Push,
            SensorTypeId = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Sensors.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new SensorDtoForCreated(entity.Id, entity.ExternalId);
    }

    public async Task<SensorDtoForDetail?> UpdateAsync(
        Guid id,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Sensors.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
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

        var entity = await context.Sensors.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        context.Sensors.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
