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

builder.AddProject<Projects.EcoData_AquaTrack_WebApp>("aquatrack-webapp")
    .WithExternalHttpEndpoints()
    .WithReference(aquatrackDb)
    .WaitFor(seeder);

builder.Build().Run();
