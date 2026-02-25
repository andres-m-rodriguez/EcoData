var builder = DistributedApplication.CreateBuilder(args);

var gateway = builder.AddProject<Projects.EcoDataPR_Gateway>("gateway");

builder.Build().Run();
