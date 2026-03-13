using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithImage("postgis/postgis", "16-3.4")
    .WithDataVolume()
    .WithPgAdmin();

var aquatrackDb = postgres.AddDatabase("aquatrack").WithDropDatabaseCommand();

var locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();

var identityDb = postgres.AddDatabase("identity").WithDropDatabaseCommand();

var seeder = builder
    .AddProject<Projects.EcoData_Seeder>("seeder")
    .WithReference(aquatrackDb)
    .WithReference(identityDb)
    .WithReference(locationsDb)
    .WaitFor(aquatrackDb)
    .WaitFor(identityDb)
    .WaitFor(locationsDb);

builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(aquatrackDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WaitFor(seeder);

builder.Build().Run();
