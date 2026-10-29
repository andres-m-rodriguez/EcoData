using Aspire.Hosting.ApplicationModel;
using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Genomics Zig App (local development only)
if (builder.ExecutionContext.IsRunMode)
{
    var genomics = builder
        .AddZigApp("genomics", "../../Features/Genomics/EcoData.Genomics.Api")
        .WithOptimization(ZigOptimizeMode.Debug)
        .WithZigHttpEndpoint(8080)
        .WithEnvironment("PORT", "8080")
        .WithExternalHttpEndpoints()
        .ExcludeFromManifest();
}

// Azure Container App Environment for deployment. The Log Analytics workspace is
// declared explicitly so aca-env references it as 'existing' and its redeploys
// never re-PUT workspace state, which is what overlapping deploy runs raced on
// ("Workspace cannot be updated while current provisioning state is not
// Succeeded"; serialized via the concurrency group in azure-dev.yml).
var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("law");
builder.AddAzureContainerAppEnvironment("aca-env").WithAzureLogAnalyticsWorkspace(logAnalytics);

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithImage("postgis/postgis", "16-3.4").WithDataVolume().WithPgAdmin());

var locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();
var wildlifeDb = postgres.AddDatabase("wildlife").WithDropDatabaseCommand();

var seeder = builder
    .AddProject<Projects.EcoData_Seeder>("seeder")
    .WithReference(locationsDb)
    .WithReference(wildlifeDb)
    .WaitFor(locationsDb)
    .WaitFor(wildlifeDb)
    .PublishAsAzureContainerAppJob();

// One-shot backfill: adds the U.S. Virgin Islands jurisdiction and the USVI occurrences of
// species already in the catalogue. Manual-trigger job; a no-op once it has run.
builder
    .AddProject<Projects.EcoData_VirginIslandsBackfill>("virgin-islands-backfill")
    .WithReference(locationsDb)
    .WithReference(wildlifeDb)
    .WaitFor(seeder)
    .PublishAsAzureContainerAppJob();

builder
    .AddProject<Projects.FaunaFinder_Server>("faunafinder")
    .WithExternalHttpEndpoints()
    .WithReference(locationsDb)
    .WithReference(wildlifeDb)
    .WaitFor(seeder)
    .PublishAsAzureContainerApp(
        (infra, app) =>
        {
            // Configure 2 max replicas
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 2;
        }
    );

builder.Build().Run();
