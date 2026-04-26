using EcoData.Common.Messaging;
using EcoData.Identity.Api.Authentication;
using EcoData.Identity.Api.Endpoints;
using EcoData.Identity.Api.RateLimiting;
using EcoData.Identity.Application.Extensions;
using EcoData.Identity.DataAccess.Extensions;
using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.Api;
using EcoData.Organization.Api.Authorization;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.Api;
using EcoData.Sensors.Api.RateLimiting;
using EcoData.Sensors.DataAccess;
using EcoData.Sensors.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using EcoPortal.Server.Components;
using EcoPortal.Server.Endpoints;
using EcoPortal.Server.Services;
using EcoPortal.Server.Workers;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();
builder.AddOrganizationDatabase();
builder.AddSensorsDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents().AddAuthenticationStateSerialization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();
builder.Services.AddIdentityDataAccess();
builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddLocationsDataAccess();
builder.Services.AddOrganizationDataAccess();
builder.Services.AddSensorsDataAccess();
builder.Services.AddWildlifeDataAccess(builder.Configuration);
builder.Services.AddMessaging(messaging =>
{
    var provider = builder.Configuration["Messaging:Provider"];
    if (string.Equals(provider, "AzureServiceBus", StringComparison.OrdinalIgnoreCase))
    {
        messaging.UseAzureServiceBus(builder.Configuration.GetSection("Messaging:ServiceBus"));
    }
    else
    {
        messaging.UseInMemoryTransport();
    }
});
builder.Services.AddScoped<INotificationRoutingService, NotificationRoutingService>();
builder.Services.AddHostedService<SensorHealthMonitorWorker>();
builder.Services.AddHostedService<NotificationDispatcherWorker>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = UserJwtAuthentication.SchemeName;
        options.DefaultChallengeScheme = UserJwtAuthentication.SchemeName;
    })
    .AddUserJwtAuthentication(builder.Configuration)
    .AddSensorJwtAuthentication(builder.Configuration);

builder.Services.AddOrganizationAuthorization();
builder.Services.AddLoginRateLimiting();
builder.Services.AddSensorReadingsRateLimiting();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseLoginRateLimiting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(EcoPortal.Client._Imports).Assembly);

app.MapOrganizationApiEndpoints();
app.MapSensorsApiEndpoints();
app.MapUserAuthEndpoints();
app.MapStateEndpoints();
app.MapMunicipalityEndpoints();
app.MapWildlifeApiEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevMessagingEndpoints();
}

app.Run();
