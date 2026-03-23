using System.Collections.Concurrent;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.IntegrationTests.Stores;

public interface ISensorsTestStore
{
    Task<SensorDtoForRegistered> GetOrCreateAsync(CancellationToken cancellationToken = default);

    Task<SensorDtoForRegistered> GetOrCreateAsync(
        string name,
        CancellationToken cancellationToken = default
    );
}

public sealed class SensorsTestStore(
    ISensorHttpClient sensorHttpClient,
    OrganizationsTestStore organizations,
    LocationsTestStore locations
) : ISensorsTestStore
{
    private readonly ConcurrentDictionary<string, SensorDtoForRegistered> _cache = new();

    public Task<SensorDtoForRegistered> GetOrCreateAsync(CancellationToken cancellationToken = default)
        => GetOrCreateAsync($"test-sensor-{Guid.NewGuid():N}", cancellationToken);

    public async Task<SensorDtoForRegistered> GetOrCreateAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        // Return cached if available (same test run or cache magically survived)
        if (_cache.TryGetValue(name, out var cached))
            return cached;

        // Check if sensor already exists from previous test run
        // Also we don't have a nice method like "GetByName" so we are doing GetSensorsAsync instead
        var parameters = new SensorParameters(
            PageSize: 10,
            Search: name,
            OrganizationId: organizations.OrganizationId
        );

        await foreach (
            var sensor in sensorHttpClient.GetSensorsAsync(parameters, cancellationToken)
        )
        {
            if (sensor.Name == name)
            {
                // Delete existing sensor - we don't have its token and currently we don't have a way to get a token or refresh it so tough luck
                await sensorHttpClient.DeleteAsync(sensor.Id, cancellationToken);
                break;
            }
        }

        // Here we register the sensor and pass the credentials down, this way we use the sensor factory to mock up a device
        var registration = await sensorHttpClient.RegisterAsync(
            new RegisterSensorRequest(
                OrganizationId: organizations.OrganizationId,
                OrganizationName: organizations.OrganizationName,
                Name: name,
                ExternalId: Guid.CreateVersion7().ToString(),
                Latitude: locations.Latitude,
                Longitude: locations.Longitude,
                MunicipalityId: locations.MunicipalityId
            ),
            cancellationToken
        );

        if (registration.IsT1)
            throw new InvalidOperationException(
                $"Failed to register sensor '{name}': {registration.AsT1.Detail}"
            );

        var credentials = registration.AsT0;
        _cache.TryAdd(name, credentials);
        return credentials;
    }
}
