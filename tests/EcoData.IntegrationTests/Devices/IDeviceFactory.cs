namespace EcoData.IntegrationTests.Devices;

public interface IDeviceFactory
{
    Esp32Device CreateEsp32Device(Guid sensorId, string accessToken);
}
