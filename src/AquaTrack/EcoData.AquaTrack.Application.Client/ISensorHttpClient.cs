using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public interface ISensorHttpClient
{
    IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );
}
