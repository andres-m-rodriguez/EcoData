using EcoData.AquaTrack.Ingestion.Models;

namespace EcoData.AquaTrack.Ingestion.Services;

public interface IUsgsApiClient
{
    Task<UsgsResponse?> GetInstantaneousValuesAsync(
        string stateCode = "PR",
        DateTimeOffset? startDt = null,
        CancellationToken cancellationToken = default);
}
