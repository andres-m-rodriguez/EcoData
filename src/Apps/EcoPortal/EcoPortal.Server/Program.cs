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
using EcoData.Organization.Api.Authentication;
using EcoData.Organization.Api.Authorization;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database.Extensions;
using EcoData.Sensors.Api;
using EcoData.Sensors.DataAccess;
using EcoData.Sensors.Database.Extensions;
using EcoPortal.Server.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();
builder.AddOrganizationDatabase();
builder.AddSensorsDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();
builder.Services.AddIdentityDataAccess();
builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddLocationsDataAccess();
builder.Services.AddOrganizationDataAccess();
builder.Services.AddSensorsDataAccess();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft
            .AspNetCore
            .Identity
            .IdentityConstants
            .ApplicationScheme;
        options.DefaultChallengeScheme = Microsoft
            .AspNetCore
            .Identity
            .IdentityConstants
            .ApplicationScheme;
    })
    .AddApiKeyAuthentication()
    .AddSensorJwtAuthentication(builder.Configuration);

builder.Services.AddOrganizationAuthorization();
builder.Services.AddLoginRateLimiting();

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

app.Run();
