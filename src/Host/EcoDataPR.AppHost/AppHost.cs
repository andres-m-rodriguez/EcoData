var builder = DistributedApplication.CreateBuilder(args);

var aquaTrackWebApp = builder.AddProject<Projects.EcoDataPR_AquaTrack_WebApp>("aquatrack-webapp");

var gateway = builder.AddProject<Projects.EcoDataPR_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(aquaTrackWebApp);

builder.Build().Run();
