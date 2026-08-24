using EcoData.Common.Authorization;
using EcoData.Identity.Application.Client;
using EcoData.Locations.Application.Client;
using EcoData.Spa.Navigation;
using EcoData.Ui;
using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using EcoPortal.Client.Features.Organizations.Services;
using EcoPortal.Client.Features.Organizations.Workspace;
using EcoPortal.Client.Layout;
using EcoPortal.Client.Services;
using EcoPortal.Client.Services.Charts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Tempest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTempest();

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
builder.Services.AddSpaNavigation();
builder.Services.AddEcoDataUi();
builder.Services.AddScoped<ITabNavigationService, TabNavigationService>();

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, EcoPortalAuthStateProvider>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<OrganizationWorkspaceState>();

// Same AddPermissions the server calls; only the source registration differs.
builder.Services.AddPermissions();
builder.Services.AddOrganizationPermissionHttpSource();

builder.Services.AddAuthorizationCore();

builder.Services.AddMudServices();

var host = builder.Build();

// Cached grants belong to one user, so an auth change drops them.
var bus = host.Services.GetRequiredService<IEventBus>();
var permissionSource = host.Services.GetRequiredService<OrganizationPermissionHttpSource>();
var workspaceState = host.Services.GetRequiredService<OrganizationWorkspaceState>();
bus.Subscribe(
    typeof(MainLayout.AuthChanged),
    _ =>
    {
        permissionSource.InvalidateCache();
        workspaceState.Invalidate();
        return Task.CompletedTask;
    }
);

await host.RunAsync();
