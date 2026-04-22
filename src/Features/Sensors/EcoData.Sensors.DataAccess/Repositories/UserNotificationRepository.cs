using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class UserNotificationRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : IUserNotificationRepository
{
    public async Task<IReadOnlyList<UserNotificationDto>> GetByUserAsync(
        Guid userId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.UserNotifications
            .Where(n => n.UserId == userId);

        if (cursor.HasValue)
        {
            query = query.Where(n => n.Id < cursor.Value);
        }

        return await query
            .OrderByDescending(n => n.Id)
            .Take(pageSize)
            .Select(n => new UserNotificationDto(
                n.Id,
                n.SensorId,
                n.Sensor!.Name,
                n.AlertId,
                n.Title,
                n.Message,
                n.Type.ToString(),
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<UserNotificationDto?> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.UserNotifications
            .AsTracking()
            .Include(n => n.Sensor)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (entity is null)
            return null;

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return new UserNotificationDto(
            entity.Id,
            entity.SensorId,
            entity.Sensor!.Name,
            entity.AlertId,
            entity.Title,
            entity.Message,
            entity.Type.ToString(),
            entity.IsRead,
            entity.CreatedAt,
            entity.ReadAt
        );
    }

    public async Task<int> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        return await context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now),
                cancellationToken
            );
    }

    public async Task<UserNotificationDto> CreateAsync(
        Guid userId,
        Guid sensorId,
        Guid? alertId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var sensor = await context.Sensors
            .Where(s => s.Id == sensorId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Sensor {sensorId} not found");

        var entity = new UserNotification
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            SensorId = sensorId,
            AlertId = alertId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ReadAt = null,
        };

        context.UserNotifications.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new UserNotificationDto(
            entity.Id,
            entity.SensorId,
            sensor.Name,
            entity.AlertId,
            entity.Title,
            entity.Message,
            entity.Type.ToString(),
            entity.IsRead,
            entity.CreatedAt,
            entity.ReadAt
        );
    }

    public async Task<IReadOnlyList<UserNotificationDto>> CreateManyAsync(
        IEnumerable<(Guid UserId, Guid SensorId, Guid? AlertId, string Title, string Message, NotificationType Type)> notifications,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var notificationList = notifications.ToList();
        if (notificationList.Count == 0)
            return [];

        var sensorIds = notificationList.Select(n => n.SensorId).Distinct().ToList();
        var sensors = await context.Sensors
            .Where(s => sensorIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entities = notificationList.Select(n => new UserNotification
        {
            Id = Guid.CreateVersion7(),
            UserId = n.UserId,
            SensorId = n.SensorId,
            AlertId = n.AlertId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = false,
            CreatedAt = now,
            ReadAt = null,
        }).ToList();

        context.UserNotifications.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        return entities.Select(e => new UserNotificationDto(
            e.Id,
            e.SensorId,
            sensors.GetValueOrDefault(e.SensorId, "Unknown"),
            e.AlertId,
            e.Title,
            e.Message,
            e.Type.ToString(),
            e.IsRead,
            e.CreatedAt,
            e.ReadAt
        )).ToList();
    }
}
