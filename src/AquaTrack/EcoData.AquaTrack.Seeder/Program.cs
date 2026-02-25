using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.Seeder;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();

builder.Services.AddHostedService<DatabaseSeederWorker>();

var host = builder.Build();
host.Run();
