namespace EcoData.IntegrationTests.Devices;

public sealed class DeviceFactory(HttpClient httpClient) : IDeviceFactory
{
    public Esp32Device CreateEsp32Device(Guid sensorId, string accessToken) =>
        new(httpClient, sensorId, accessToken);
}
