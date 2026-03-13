using EcoData.Sensors.Ingestion.Models;

namespace EcoData.Sensors.Ingestion.Services;

public interface IUsgsApiClient
{
    Task<UsgsResponse?> GetInstantaneousValuesAsync(
        string stateCode = "PR",
        DateTimeOffset? startDt = null,
        CancellationToken cancellationToken = default);
}
