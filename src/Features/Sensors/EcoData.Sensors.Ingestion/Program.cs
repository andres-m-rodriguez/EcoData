using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.DataAccess;
using EcoData.Sensors.Database.Extensions;
using EcoData.Sensors.Ingestion.Services;
using EcoData.Sensors.Ingestion.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrganizationDatabase();
builder.Services.AddOrganizationDataAccess();
builder.AddSensorsDatabase();
builder.Services.AddSensorsDataAccess();
builder.AddLocationsDatabase();
builder.Services.AddLocationsDataAccess();

builder.Services.AddHttpClient<IUsgsApiClient, UsgsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://waterservices.usgs.gov/nwis/iv/");
});
builder.Services.AddHostedService<UsgsIngestionWorker>();
builder.Services.AddHostedService<SensorHealthMonitorWorker>();

var host = builder.Build();
host.Run();
