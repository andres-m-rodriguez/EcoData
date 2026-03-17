namespace EcoData.IntegrationTests;

public interface IDeviceFactory
{
    Esp32Device CreateEsp32Device();
}

public sealed class DeviceFactory(HttpClient httpClient) : IDeviceFactory
{
    public Esp32Device CreateEsp32Device() => new(httpClient);
}
