using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface IUserSubscriptionHttpClient
{
    Task<OneOf<IReadOnlyList<UserSensorSubscriptionDto>, RequestFailed>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default
    );

    // A 404 RequestFailed means the user is not subscribed to the sensor.
    Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> GetSubscriptionAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> SubscribeAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> UpdateSubscriptionAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<OneOf.Types.Success, RequestFailed>> UnsubscribeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
