using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class UserSensorSubscriptionRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IUserSensorSubscriptionRepository
{
    public async Task<UserSensorSubscriptionDto?> GetAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.UserSensorSubscriptions
            .Where(s => s.UserId == userId && s.SensorId == sensorId)
            .Select(s => new UserSensorSubscriptionDto(
                s.Id,
                s.SensorId,
                s.Sensor!.Name,
                s.NotifyOnStale,
                s.NotifyOnUnhealthy,
                s.NotifyOnRecovered,
                s.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSensorSubscriptionDto>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.UserSensorSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserSensorSubscriptionDto(
                s.Id,
                s.SensorId,
                s.Sensor!.Name,
                s.NotifyOnStale,
                s.NotifyOnUnhealthy,
                s.NotifyOnRecovered,
                s.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSubscribedUserIdsAsync(
        Guid sensorId,
        bool stale,
        bool unhealthy,
        bool recovered,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.UserSensorSubscriptions
            .Where(s => s.SensorId == sensorId);

        if (stale)
            query = query.Where(s => s.NotifyOnStale);
        else if (unhealthy)
            query = query.Where(s => s.NotifyOnUnhealthy);
        else if (recovered)
            query = query.Where(s => s.NotifyOnRecovered);

        return await query
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSensorSubscriptionDto> CreateAsync(
        Guid userId,
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate dto,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var sensor = await context.Sensors
            .Where(s => s.Id == sensorId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Sensor {sensorId} not found");

        var entity = new UserSensorSubscription
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            SensorId = sensorId,
            NotifyOnStale = dto.NotifyOnStale,
            NotifyOnUnhealthy = dto.NotifyOnUnhealthy,
            NotifyOnRecovered = dto.NotifyOnRecovered,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.UserSensorSubscriptions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new UserSensorSubscriptionDto(
            entity.Id,
            entity.SensorId,
            sensor.Name,
            entity.NotifyOnStale,
            entity.NotifyOnUnhealthy,
            entity.NotifyOnRecovered,
            entity.CreatedAt
        );
    }

    public async Task<UserSensorSubscriptionDto?> UpdateAsync(
        Guid userId,
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate dto,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.UserSensorSubscriptions
            .AsTracking()
            .Include(s => s.Sensor)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SensorId == sensorId, cancellationToken);

        if (entity is null)
            return null;

        entity.NotifyOnStale = dto.NotifyOnStale;
        entity.NotifyOnUnhealthy = dto.NotifyOnUnhealthy;
        entity.NotifyOnRecovered = dto.NotifyOnRecovered;

        await context.SaveChangesAsync(cancellationToken);

        return new UserSensorSubscriptionDto(
            entity.Id,
            entity.SensorId,
            entity.Sensor!.Name,
            entity.NotifyOnStale,
            entity.NotifyOnUnhealthy,
            entity.NotifyOnRecovered,
            entity.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.UserSensorSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SensorId == sensorId, cancellationToken);

        if (entity is null)
            return false;

        context.UserSensorSubscriptions.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.UserSensorSubscriptions
            .AnyAsync(s => s.UserId == userId && s.SensorId == sensorId, cancellationToken);
    }
}
