using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface IUserSubscriptionHttpClient
{
    Task<IReadOnlyList<UserSensorSubscriptionDto>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default
    );

    Task<UserSensorSubscriptionDto?> GetSubscriptionAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserSensorSubscriptionDto, ProblemDetail>> SubscribeAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserSensorSubscriptionDto, ProblemDetail>> UpdateSubscriptionAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<bool, ProblemDetail>> UnsubscribeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
