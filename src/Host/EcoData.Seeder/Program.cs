using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.Database.Extensions;
using EcoData.Seeder;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrganizationDatabase();
builder.AddSensorsDatabase();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();

// Species profile images are written to blob storage, not to the wildlife
// database. Only the image store is needed here — the seeder writes through the
// DbContexts directly and never touches a repository.
builder.AddAzureBlobServiceClient("images");
builder.Services.AddWildlifeImageStorage();

builder.Services.AddHostedService<DatabaseSeederWorker>();

var host = builder.Build();
host.Run();
