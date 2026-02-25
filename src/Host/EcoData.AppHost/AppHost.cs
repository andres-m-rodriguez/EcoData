var builder = DistributedApplication.CreateBuilder(args);

var aquaTrackWebApp = builder.AddProject<Projects.EcoData_AquaTrack_WebApp>("aquatrack-webapp");

var gateway = builder.AddProject<Projects.EcoData_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(aquaTrackWebApp);

builder.Build().Run();
