using EcoData.Identity.Contracts.Responses;

namespace EcoData.Identity.Application.Server.Services;

public interface ISensorIdentityProviderService
{
    Task<SensorProvisionResponse> ProvisionAsync(
        Guid sensorId,
        Guid organizationId,
        string organizationName,
        string sensorName,
        CancellationToken cancellationToken = default
    );

    Task<SensorTokenResponse?> RefreshTokenAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsProvisionedAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task RevokeAsync(Guid sensorId, CancellationToken cancellationToken = default);
}
