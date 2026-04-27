using Aspire.Hosting.ApplicationModel;
using EcoData.AppHost.Extensions;
using EcoData.Sensors.Contracts.Events;

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

// Custom domain + managed certificate name for ecoportal. Both flow in via
// Parameters__customDomain / Parameters__certificateName env vars in CI. Baking
// these into the Aspire-emitted Bicep keeps ingress.customDomains populated on
// every redeploy — without it, ACA wipes the binding when the property is
// omitted (microsoft/azure-container-apps#957) and the site goes dark until a
// post-deploy rebind.
var customDomain = builder.AddParameter("customDomain");
var certificateName = builder.AddParameter("certificateName");

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithImage("postgis/postgis", "16-3.4").WithDataVolume().WithPgAdmin());

var organizationDb = postgres.AddDatabase("organization").WithDropDatabaseCommand();
var sensorsDb = postgres.AddDatabase("sensors").WithDropDatabaseCommand();
var locationsDb = postgres.AddDatabase("locations").WithDropDatabaseCommand();
var identityDb = postgres.AddDatabase("identity").WithDropDatabaseCommand();
var wildlifeDb = postgres.AddDatabase("wildlife").WithDropDatabaseCommand();

// Azure Service Bus — runs as the official Microsoft emulator locally,
// provisions a real namespace in publish mode. Topic + subscription are
// declared here so they exist in both environments without runtime admin calls.
var serviceBus = builder
    .AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var eventsTopic = serviceBus.AddServiceBusTopic("ecodata-events");
// One subscription per event type. The transport derives the subscription name from
// typeof(T).Name.ToLowerInvariant(); each event record exposes the same name as a
// SubscriptionName constant from its module's contracts library. Add an entry here for
// every type used with IMessageBus.
// Sensors module
eventsTopic.AddServiceBusSubscription(ReadingCreatedEvent.SubscriptionName);
eventsTopic.AddServiceBusSubscription(SensorHealthAlertEvent.SubscriptionName);
eventsTopic.AddServiceBusSubscription(UserNotificationEvent.SubscriptionName);

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

var ecoportal = builder
    .AddProject<Projects.EcoPortal_Server>("ecoportal")
    .WithExternalHttpEndpoints()
    .WithReference(organizationDb)
    .WithReference(sensorsDb)
    .WithReference(locationsDb)
    .WithReference(identityDb)
    .WithReference(wildlifeDb)
    .WithReference(serviceBus)
    .WaitFor(eventsTopic)
    .WithEnvironment("Jwt__SensorSecretKey", jwtSensorSecretKey)
    .WithEnvironment("Jwt__UserSecretKey", jwtUserSecretKey)
    .WithEnvironment("Jwt__Issuer", "EcoData")
    .WithEnvironment("Jwt__Audience", "EcoData")
    .WithEnvironment("Jwt__ExpirationHours", "24")
    .WithEnvironment("Messaging__ServiceBus__ConnectionString", serviceBus.Resource.ConnectionStringExpression)
    .WithEnvironment("Messaging__ServiceBus__TopicName", "ecodata-events")
    .WaitFor(seeder)
    .PublishAsAzureContainerApp(
        (infra, app) =>
        {
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 2;
            app.ConfigureCustomDomain(customDomain, certificateName);
        }
    );

var faunafinder = builder
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
