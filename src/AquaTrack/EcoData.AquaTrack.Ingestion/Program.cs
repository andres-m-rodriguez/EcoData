using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.Ingestion.Services;
using EcoData.AquaTrack.Ingestion.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();

builder.Services.AddHttpClient<IUsgsApiClient, UsgsApiClient>();
builder.Services.AddHostedService<UsgsIngestionWorker>();

var host = builder.Build();
host.Run();
