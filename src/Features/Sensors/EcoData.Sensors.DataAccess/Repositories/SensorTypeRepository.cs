using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Database;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class SensorTypeRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISensorTypeRepository
{
    public async Task<IReadOnlyList<SensorTypeDtoForList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SensorTypes
            .OrderBy(st => st.Name)
            .Select(st => new SensorTypeDtoForList(
                st.Id,
                st.Code,
                st.Name,
                st.Description,
                st.Parameters.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<SensorTypeDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SensorTypes
            .Where(st => st.Id == id)
            .Select(st => new SensorTypeDtoForDetail(
                st.Id,
                st.Code,
                st.Name,
                st.Description,
                st.CreatedAt,
                st.Parameters.Select(p => new ParameterDtoForList(
                    p.Id,
                    p.SourceId,
                    p.Code,
                    p.Name,
                    p.DefaultUnit,
                    p.SensorTypeId,
                    st.Name,
                    p.PhenomenonId,
                    p.Phenomenon!.Code,
                    p.SourceUnit,
                    p.UnitFactor,
                    p.UnitOffset,
                    p.ValueShape.ToString()
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SensorTypeDtoForDetail?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SensorTypes
            .Where(st => st.Code == code)
            .Select(st => new SensorTypeDtoForDetail(
                st.Id,
                st.Code,
                st.Name,
                st.Description,
                st.CreatedAt,
                st.Parameters.Select(p => new ParameterDtoForList(
                    p.Id,
                    p.SourceId,
                    p.Code,
                    p.Name,
                    p.DefaultUnit,
                    p.SensorTypeId,
                    st.Name,
                    p.PhenomenonId,
                    p.Phenomenon!.Code,
                    p.SourceUnit,
                    p.UnitFactor,
                    p.UnitOffset,
                    p.ValueShape.ToString()
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class ParameterRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IParameterRepository
{
    public async Task<IReadOnlyList<ParameterDtoForList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Parameters
            .OrderBy(p => p.Name)
            .Select(p => new ParameterDtoForList(
                p.Id,
                p.SourceId,
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType != null ? p.SensorType.Name : null,
                p.PhenomenonId,
                p.Phenomenon!.Code,
                p.SourceUnit,
                p.UnitFactor,
                p.UnitOffset,
                p.ValueShape.ToString()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParameterDtoForList>> GetBySensorTypeAsync(
        Guid sensorTypeId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Parameters
            .Where(p => p.SensorTypeId == sensorTypeId)
            .OrderBy(p => p.Name)
            .Select(p => new ParameterDtoForList(
                p.Id,
                p.SourceId,
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType != null ? p.SensorType.Name : null,
                p.PhenomenonId,
                p.Phenomenon!.Code,
                p.SourceUnit,
                p.UnitFactor,
                p.UnitOffset,
                p.ValueShape.ToString()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<ParameterDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Parameters
            .Where(p => p.Id == id)
            .Select(p => new ParameterDtoForDetail(
                p.Id,
                p.SourceId,
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType != null ? p.SensorType.Name : null,
                p.PhenomenonId,
                p.Phenomenon!.Code,
                p.SourceUnit,
                p.UnitFactor,
                p.UnitOffset,
                p.ValueShape.ToString(),
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ParameterDtoForDetail?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Parameters
            .Where(p => p.Code == code)
            .Select(p => new ParameterDtoForDetail(
                p.Id,
                p.SourceId,
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType != null ? p.SensorType.Name : null,
                p.PhenomenonId,
                p.Phenomenon!.Code,
                p.SourceUnit,
                p.UnitFactor,
                p.UnitOffset,
                p.ValueShape.ToString(),
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
