using EcoData.AquaTrack.Contracts.Dtos;
using Microsoft.JSInterop;

namespace EcoData.AquaTrack.WebApp.Client.Services;

public sealed class SensorMapManager(IJSRuntime js) : ISensorMapManager
{
    public ValueTask InitializeAsync(string elementId)
    {
        return js.InvokeVoidAsync("sensorMap.init", elementId);
    }

    public ValueTask AddSensorsAsync(IEnumerable<SensorDtoForList> sensors)
    {
        return js.InvokeVoidAsync("sensorMap.addSensors", sensors);
    }

    public ValueTask DisposeAsync()
    {
        return js.InvokeVoidAsync("sensorMap.dispose");
    }
}
