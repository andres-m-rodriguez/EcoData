using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// JWT secret - from Key Vault in production, parameter in development
var jwtSecretKey = builder.AddParameter("jwt-secret-key", secret: true);

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication()
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

var sensorsIngestion = builder
    .AddProject<Projects.EcoData_Sensors_Ingestion>("sensors-ingestion")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WaitFor(seeder);

// Azure Key Vault for production secrets
if (builder.ExecutionContext.IsPublishMode)
{
    var keyVault = builder.AddAzureKeyVault("keyvault");
    ecoportal.WithReference(keyVault);
    sensorsIngestion.WithReference(keyVault);
}

// Pipeline steps for Azure deployment
builder.AddMigrationsStep();

builder.Build().Run();
