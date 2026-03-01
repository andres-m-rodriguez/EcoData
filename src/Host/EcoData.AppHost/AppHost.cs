using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis", "16-3.4")
    .WithDataVolume()
    .WithPgAdmin();

var aquatrackDb = postgres.AddDatabase("aquatrack")
    .WithDropDatabaseCommand();

var locationsDb = postgres.AddDatabase("locations")
    .WithDropDatabaseCommand();

var aquatrackSeeder = builder.AddProject<Projects.EcoData_AquaTrack_Seeder>("aquatrack-seeder")
    .WithReference(aquatrackDb)
    .WaitFor(aquatrackDb);

var locationsSeeder = builder.AddProject<Projects.EcoData_Locations_Seeder>("locations-seeder")
    .WithReference(locationsDb)
    .WaitFor(locationsDb);

var ingestion = builder.AddProject<Projects.EcoData_AquaTrack_Ingestion>("aquatrack-ingestion")
    .WithReference(aquatrackDb)
    .WaitFor(aquatrackSeeder);

builder.AddProject<Projects.EcoData_AquaTrack_WebApp>("aquatrack-webapp")
    .WithExternalHttpEndpoints()
    .WithReference(aquatrackDb)
    .WithReference(locationsDb)
    .WaitFor(aquatrackSeeder)
    .WaitFor(locationsSeeder);

builder.Build().Run();
