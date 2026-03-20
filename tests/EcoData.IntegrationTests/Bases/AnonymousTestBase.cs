using EcoData.Identity.Application.Client.HttpClients;
using EcoData.IntegrationTests.Devices;
using EcoData.IntegrationTests.Stores;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using Xunit;

namespace EcoData.IntegrationTests.Bases;

[Collection(EcoDataTestCollection.Name)]
public abstract class AnonymousTestBase(EcoDataTestFixture fixture)
{
    protected HttpClient HttpClient => fixture.HttpClient;
    protected IAuthHttpClient AuthHttpClient => fixture.AuthHttpClient;
    protected IOrganizationHttpClient OrganizationHttpClient => fixture.OrganizationHttpClient;
    protected ISensorHttpClient SensorHttpClient => fixture.SensorHttpClient;
    protected ISensorReadingHttpClient SensorReadingHttpClient => fixture.SensorReadingHttpClient;
    protected ISensorHealthHttpClient SensorHealthHttpClient => fixture.SensorHealthHttpClient;
    protected OrganizationsTestStore Organizations => fixture.Organizations;
    protected LocationsTestStore Locations => fixture.Locations;
    protected IDeviceFactory DeviceFactory => fixture.DeviceFactory;
}
