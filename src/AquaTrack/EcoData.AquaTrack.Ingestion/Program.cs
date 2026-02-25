using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.DataAccess.Extensions;
using EcoData.AquaTrack.Ingestion.Services;
using EcoData.AquaTrack.Ingestion.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();
builder.Services.AddAquaTrackDataAccess();

builder.Services.AddHttpClient<IUsgsApiClient, UsgsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://waterservices.usgs.gov/nwis/iv/");
});
builder.Services.AddHostedService<UsgsIngestionWorker>();

var host = builder.Build();
host.Run();
