using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class SensorTypeRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
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
                    p.Code,
                    p.Name,
                    p.DefaultUnit,
                    p.SensorTypeId,
                    st.Name
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
                    p.Code,
                    p.Name,
                    p.DefaultUnit,
                    p.SensorTypeId,
                    st.Name
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class ParameterRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IParameterRepository
{
    public async Task<IReadOnlyList<ParameterDtoForList>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Parameters
            .OrderBy(p => p.Name)
            .Select(p => new ParameterDtoForList(
                p.Id,
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType!.Name
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
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType!.Name
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
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType!.Name,
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
                p.Code,
                p.Name,
                p.DefaultUnit,
                p.SensorTypeId,
                p.SensorType!.Name,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
