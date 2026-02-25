using EcoData.AquaTrack.Ingestion.Models;

namespace EcoData.AquaTrack.Ingestion.Services;

public interface IUsgsApiClient
{
    Task<UsgsResponse?> GetInstantaneousValuesAsync(
        string stateCode = "PR",
        string period = "P1D",
        CancellationToken cancellationToken = default);
}
