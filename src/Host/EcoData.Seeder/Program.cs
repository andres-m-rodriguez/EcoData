using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.Database.Extensions;
using EcoData.Seeder;
using EcoData.Wildlife.Database.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrganizationDatabase();
builder.AddSensorsDatabase();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddHostedService<DatabaseSeederWorker>();

var host = builder.Build();
host.Run();
