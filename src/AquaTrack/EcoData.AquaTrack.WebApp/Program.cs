using EcoData.AquaTrack.Api;
using EcoData.AquaTrack.Api.Authentication;
using EcoData.AquaTrack.Api.Authorization;
using EcoData.AquaTrack.DataAccess.Extensions;
using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.WebApp.Components;
using EcoData.Identity.Api;
using EcoData.Identity.Application.Extensions;
using EcoData.Identity.DataAccess.Extensions;
using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
builder.Services.AddAquaTrackDataAccess();
builder.Services.AddIdentityDataAccess();
builder.Services.AddIdentityApplication();
builder.Services.AddLocationsDataAccess();

builder.Services.AddAuthentication().AddApiKeyAuthentication();

builder.Services.AddAquaTrackAuthorization();

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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(EcoData.AquaTrack.WebApp.Client._Imports).Assembly);

app.MapSensorEndpoints();
app.MapSensorHealthEndpoints();
app.MapDataSourceEndpoints();
app.MapOrganizationEndpoints();
app.MapMemberEndpoints();
app.MapApiKeyEndpoints();
app.MapPushEndpoints();
app.MapReferenceDataEndpoints();
app.MapAuthEndpoints();
app.MapStateEndpoints();
app.MapMunicipalityEndpoints();

app.Run();
