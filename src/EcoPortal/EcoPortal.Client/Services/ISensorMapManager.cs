using EcoData.Sensors.Contracts.Dtos;

namespace EcoPortal.Client.Services;

public interface ISensorMapManager
{
    ValueTask InitializeAsync(string elementId);
    ValueTask AddSensorsAsync(IEnumerable<SensorDtoForList> sensors);
    ValueTask DisposeAsync();
}
