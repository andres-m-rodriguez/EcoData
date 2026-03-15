using EcoData.Organization.Application.Client;
using EcoData.Sensors.Application.Client;
using EcoPortal.Client.Authorization;
using EcoPortal.Client.Services;
using EcoData.Identity.Application.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddOrganizationClient(baseAddress);
builder.Services.AddSensorsClient(baseAddress);
builder.Services.AddIdentityClient(baseAddress);

builder.Services.AddHttpClient<ILocationHttpClient, LocationHttpClient>(client =>
{
    client.BaseAddress = baseAddress;
});

builder.Services.AddHttpClient<IDataSourceHttpClient, DataSourceHttpClient>(client =>
{
    client.BaseAddress = baseAddress;
});

builder.Services.AddScoped<ISensorMapManager, SensorMapManager>();
builder.Services.AddScoped<ILeafletMapService, LeafletMapService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<PermissionContextService>();
builder.Services.AddScoped<ClientAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ClientAuthStateProvider>()
);
// Register custom policy provider BEFORE AddAuthorizationCore (uses TryAddSingleton)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, OrganizationPermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationPermissionHandler>();
builder.Services.AddIdentityAuthorization();

builder.Services.AddMudServices();

await builder.Build().RunAsync();
