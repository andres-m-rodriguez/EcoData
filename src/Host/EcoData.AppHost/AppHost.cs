using EcoData.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Azure Key Vault for production secrets
var keyVault = builder.AddAzureKeyVault("keyvault");

// JWT secret - from Key Vault in production, parameter in development
var jwtSecretKey = builder.AddParameter("jwt-secret-key", secret: true);

// PostgreSQL configuration - Azure Flexible Server in production, local container in development
IResourceBuilder<PostgresDatabaseResource> organizationDb;
IResourceBuilder<PostgresDatabaseResource> sensorsDb;
IResourceBuilder<PostgresDatabaseResource> locationsDb;
IResourceBuilder<PostgresDatabaseResource> identityDb;

if (builder.ExecutionContext.IsPublishMode)
{
    // Azure PostgreSQL Flexible Server for production
    var postgres = builder
        .AddAzurePostgresFlexibleServer("postgres")
        .WithPasswordAuthentication()
        .RunAsContainer(c => c
            .WithImage("postgis/postgis", "16-3.4")
            .WithPgAdmin());

    organizationDb = postgres.AddDatabase("organization");
    sensorsDb = postgres.AddDatabase("sensors");
    locationsDb = postgres.AddDatabase("locations");
    identityDb = postgres.AddDatabase("identity");
}
else
{
    // Local PostgreSQL container for development
    var postgres = builder
        .AddPostgres("postgres")
        .WithImage("postgis/postgis", "16-3.4")
        .WithDataVolume()
        .WithPgAdmin();

    organizationDb = postgres.AddDatabase("organization").WithDropDatabaseCommand();
    sensorsDb = postgres.AddDatabase("sensors").WithDropDatabaseCommand();
    locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();
    identityDb = postgres.AddDatabase("identity").WithDropDatabaseCommand();
}

var seeder = builder
    .AddProject<Projects.EcoData_Seeder>("seeder")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(identityDb)
    .WithReference(locationsDb)
    .WaitFor(organizationDb)
    .WaitFor(sensorsDb)
    .WaitFor(identityDb)
    .WaitFor(locationsDb);

var ecoportal = builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WithReference(keyVault)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", "EcoData")
    .WithEnvironment("Jwt__Audience", "EcoData")
    .WithEnvironment("Jwt__ExpirationHours", "24")
    .WaitFor(seeder);

builder
    .AddProject<Projects.EcoData_Sensors_Ingestion>("sensors-ingestion")
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(keyVault)
    .WaitFor(seeder);

builder.Build().Run();
