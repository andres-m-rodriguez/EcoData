using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.DataAccess;
using EcoData.Sensors.Database.Extensions;
using EcoData.Sensors.Ingestion.Seeders;
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

// Phenomenon + USGS parameter mapping seeder runs before the worker so canonical resolution is ready on first ingest.
// Backfill resolves canonical values on any readings ingested before the mappings existed. Both run once at startup.
builder.Services.AddHostedService<PhenomenonSeeder>();
builder.Services.AddHostedService<ReadingBackfillService>();
builder.Services.AddHostedService<UsgsIngestionWorker>();

var host = builder.Build();
host.Run();
