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

// Azure Container App Environment for deployment
builder.AddAzureContainerAppEnvironment("aca-env");

// JWT secrets - from Key Vault in production, parameters in development
var jwtSensorSecretKey = builder.AddParameter("jwt-sensor-secret-key", secret: true);
var jwtUserSecretKey = builder.AddParameter("jwt-user-secret-key", secret: true);

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithImage("postgis/postgis", "16-3.4").WithDataVolume().WithPgAdmin());

var organizationDb = postgres.AddDatabase("organization").WithDropDatabaseCommand();
var sensorsDb = postgres.AddDatabase("sensors").WithDropDatabaseCommand();
var locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();
var identityDb = postgres.AddDatabase("identity").WithDropDatabaseCommand();
var wildlifeDb = postgres.AddDatabase("wildlife").WithDropDatabaseCommand();

var seeder = builder
    .AddProject<Projects.EcoData_Seeder>("seeder")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(identityDb)
    .WithReference(locationsDb)
    .WithReference(wildlifeDb)
    .WaitFor(organizationDb)
    .WaitFor(sensorsDb)
    .WaitFor(identityDb)
    .WaitFor(locationsDb)
    .WaitFor(wildlifeDb)
    .PublishAsAzureContainerAppJob();

if (builder.Environment.EnvironmentName == "Testing")
{
    seeder.WithEnvironment("SEED_TEST_DATA", "true");
}

// Custom domain is configured via GitHub Actions workflow step after deployment
// to avoid Aspire resetting the SSL binding during re-provisioning

var ecoportal = builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WithReference(wildlifeDb)
    .WithEnvironment("Jwt__SensorSecretKey", jwtSensorSecretKey)
    .WithEnvironment("Jwt__UserSecretKey", jwtUserSecretKey)
    .WithEnvironment("Jwt__Issuer", "EcoData")
    .WithEnvironment("Jwt__Audience", "EcoData")
    .WithEnvironment("Jwt__ExpirationHours", "24")
    .WaitFor(seeder)
    .PublishAsAzureContainerApp(
        (infra, app) =>
        {
            // Configure 2 max replicas
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 2;
        }
    );

// FaunaFinder app (local development only)
var faunafinder = builder
    .AddProject<Projects.FaunaFinder_Server>("faunafinder")
    .WithReference(locationsDb)
    .WithReference(wildlifeDb)
    .WaitFor(seeder)
    .ExcludeFromManifest();

var sensorsIngestion = builder
    .AddProject<Projects.EcoData_Sensors_Ingestion>("sensors-ingestion")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WaitFor(seeder);

// Azure resources for production
if (builder.ExecutionContext.IsPublishMode)
{
    var keyVault = builder.AddAzureKeyVault("keyvault");
    ecoportal.WithReference(keyVault);
    sensorsIngestion.WithReference(keyVault);

    // Application Insights for telemetry
    var appInsights = builder.AddAzureApplicationInsights("appinsights");
    ecoportal.WithReference(appInsights);
    sensorsIngestion.WithReference(appInsights);
    seeder.WithReference(appInsights);
}

builder.Build().Run();
