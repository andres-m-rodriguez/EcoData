using EcoData.AquaTrack.DataAccess.Extensions;
using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.Ingestion.Services;
using EcoData.AquaTrack.Ingestion.Workers;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();
builder.Services.AddAquaTrackDataAccess();
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
