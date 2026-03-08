using EcoData.AquaTrack.Database.Extensions;
using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Seeder;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();

builder.Services.AddHostedService<DatabaseSeederWorker>();

var host = builder.Build();
host.Run();
