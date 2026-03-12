using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.WebApp.Client.Services;

public interface ISensorMapManager
{
    ValueTask InitializeAsync(string elementId);
    ValueTask AddSensorsAsync(IEnumerable<SensorDtoForList> sensors);
    ValueTask DisposeAsync();
}
