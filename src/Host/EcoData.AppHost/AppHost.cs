using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithImage("postgis/postgis", "16-3.4")
    .WithDataVolume()
    .WithPgAdmin();

var organizationDb = postgres.AddDatabase("organization").WithDropDatabaseCommand();

var sensorsDb = postgres.AddDatabase("sensors").WithDropDatabaseCommand();

var locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();

var identityDb = postgres.AddDatabase("identity").WithDropDatabaseCommand();

var seeder = builder
    .AddProject<Projects.EcoData_Seeder>("seeder")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(identityDb)
    .WithReference(locationsDb)
    .WaitFor(organizationDb)
    .WaitFor(sensorsDb)
    .WaitFor(identityDb)
    .WaitFor(locationsDb);

builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WaitFor(seeder);

builder
    .AddProject<Projects.EcoData_Sensors_Ingestion>("sensors-ingestion")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WaitFor(seeder);

builder.Build().Run();
