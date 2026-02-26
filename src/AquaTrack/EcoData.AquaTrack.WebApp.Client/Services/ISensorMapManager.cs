using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.WebApp.Client.Services;

public interface ISensorMapManager
{
    ValueTask InitializeAsync(string elementId);
    ValueTask AddSensorsAsync(IEnumerable<SensorDtoForList> sensors);
    ValueTask DisposeAsync();
}
