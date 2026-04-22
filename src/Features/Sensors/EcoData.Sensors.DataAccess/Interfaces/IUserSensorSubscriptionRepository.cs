using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IUserSensorSubscriptionRepository
{
    Task<UserSensorSubscriptionDto?> GetAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<UserSensorSubscriptionDto>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Guid>> GetSubscribedUserIdsAsync(
        Guid sensorId,
        bool stale,
        bool unhealthy,
        bool recovered,
        CancellationToken cancellationToken = default
    );

    Task<UserSensorSubscriptionDto> CreateAsync(
        Guid userId,
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<UserSensorSubscriptionDto?> UpdateAsync(
        Guid userId,
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        Guid userId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
