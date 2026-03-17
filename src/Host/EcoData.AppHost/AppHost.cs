#pragma warning disable ASPIREACADOMAINS001

using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Azure Container App Environment for deployment
builder.AddAzureContainerAppEnvironment("aca-env");

// JWT secret - from Key Vault in production, parameter in development
var jwtSecretKey = builder.AddParameter("jwt-secret-key", secret: true);

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithImage("postgis/postgis", "16-3.4").WithDataVolume().WithPgAdmin());

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
    .WaitFor(locationsDb)
    .PublishAsAzureContainerAppJob();

var customDomainValue = builder.Configuration["Parameters:customDomain"];
var hasCustomDomain = !string.IsNullOrEmpty(customDomainValue);

var ecoportal = builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", "EcoData")
    .WithEnvironment("Jwt__Audience", "EcoData")
    .WithEnvironment("Jwt__ExpirationHours", "24")
    .WaitFor(seeder);

if (hasCustomDomain)
{
    var customDomain = builder.AddParameter("customDomain");
    var certificateName = builder.AddParameter("certificateName", "", publishValueAsDefault: true);

    ecoportal.PublishAsAzureContainerApp(
        (infra, app) =>
        {
            app.ConfigureCustomDomain(customDomain, certificateName);
        }
    );
}

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

// Pipeline steps for Azure deployment
builder.AddMigrationsStep();
builder.AddCustomDomainStep();

builder.Build().Run();
