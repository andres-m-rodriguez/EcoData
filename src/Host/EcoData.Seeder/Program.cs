using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.Database.Extensions;
using EcoData.Seeder;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrganizationDatabase("aquatrack");
builder.AddSensorsDatabase("aquatrack");
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();

builder.Services.AddHostedService<DatabaseSeederWorker>();

var host = builder.Build();
host.Run();
