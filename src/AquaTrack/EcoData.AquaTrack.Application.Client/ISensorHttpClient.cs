using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public interface ISensorHttpClient
{
    IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<SensorDtoForDetail?> GetByIdAsync(
        Guid organizationId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<SensorDtoForCreated?> CreateForOrganizationAsync(
        Guid organizationId,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<SensorDtoForDetail?> UpdateAsync(
        Guid organizationId,
        Guid sensorId,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid organizationId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
