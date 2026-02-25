var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var aquatrackDb = postgres.AddDatabase("aquatrack");

var seeder = builder.AddProject<Projects.EcoData_AquaTrack_Seeder>("aquatrack-seeder")
    .WithReference(aquatrackDb)
    .WaitFor(aquatrackDb);

var ingestion = builder.AddProject<Projects.EcoData_AquaTrack_Ingestion>("aquatrack-ingestion")
    .WithReference(aquatrackDb)
    .WaitFor(seeder);

var aquaTrackWebApp = builder.AddProject<Projects.EcoData_AquaTrack_WebApp>("aquatrack-webapp")
    .WithReference(aquatrackDb)
    .WaitFor(seeder);

var gateway = builder.AddProject<Projects.EcoData_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(aquaTrackWebApp);

builder.Build().Run();
