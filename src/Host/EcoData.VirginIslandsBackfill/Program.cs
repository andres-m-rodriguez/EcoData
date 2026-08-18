using EcoData.Locations.Database.Extensions;
using EcoData.VirginIslandsBackfill;
using EcoData.Wildlife.Database.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddHostedService<VirginIslandsBackfillWorker>();

var host = builder.Build();
host.Run();
