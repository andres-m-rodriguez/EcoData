using EcoData.Identity.Application.Client;
using EcoData.Locations.Application.Client;
using EcoData.NativeUi;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using EcoPortal.Client.Authorization;
using EcoPortal.Client.Features.Organizations.Services;
using EcoPortal.Client.Services;
using EcoPortal.Client.Services.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddOrganizationClient(baseAddress);
builder.Services.AddSensorsClient(baseAddress);
builder.Services.AddIdentityClient(baseAddress);
builder.Services.AddLocationsClient(baseAddress);

builder.Services.AddHttpClient<ILocationHttpClient, LocationHttpClient>(client =>
{
    client.BaseAddress = baseAddress;
});

builder.Services.AddHttpClient<IDataSourceHttpClient, DataSourceHttpClient>(client =>
{
    client.BaseAddress = baseAddress;
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IOrganizationCacheService, OrganizationCacheService>();
builder.Services.AddScoped<ILeafletMapService, LeafletMapService>();
builder.Services.AddScoped<IChartService, ChartService>();

// Navigation services
builder.Services.AddNativeUi();
builder.Services.AddScoped<ITabNavigationService, TabNavigationService>();

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, EcoPortalAuthStateProvider>();
builder.Services.AddScoped<PermissionContextService>();
builder.Services.AddScoped<NotificationService>();

// Register custom policy provider BEFORE AddAuthorizationCore (uses TryAddSingleton)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, OrganizationPermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationPermissionHandler>();
builder.Services.AddAuthorizationCore();

builder.Services.AddMudServices();

await builder.Build().RunAsync();
