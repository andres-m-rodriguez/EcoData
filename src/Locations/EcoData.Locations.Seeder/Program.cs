using EcoData.Locations.Database.Extensions;
using EcoData.Locations.Seeder;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddLocationsDatabase();

builder.Services.AddHostedService<LocationsSeederWorker>();

var host = builder.Build();
host.Run();
