var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var aquatrackDb = postgres.AddDatabase("aquatrack");

var aquaTrackWebApp = builder.AddProject<Projects.EcoData_AquaTrack_WebApp>("aquatrack-webapp")
    .WithReference(aquatrackDb)
    .WaitFor(aquatrackDb);

var gateway = builder.AddProject<Projects.EcoData_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(aquaTrackWebApp);

builder.Build().Run();
