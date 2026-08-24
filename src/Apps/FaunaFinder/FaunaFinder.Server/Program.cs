using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using FaunaFinder.Server.Components;
using FaunaFinder.Server.Mcp;
using FaunaFinder.Server.RateLimiting;
using MudBlazor.Services;
using Tempest;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddTempest();
builder.Services.AddLocationsDataAccess();
builder.Services.AddWildlifeDataAccess(builder.Configuration);
builder.Services.AddFaunaFinderMcp();
builder.Services.AddFaunaFinderRateLimiting();

var app = builder.Build();

// First in the pipeline: everything downstream that cares who is calling —
// the rate limiter above all — reads the address this restores.
app.UseForwardedHeaders();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseAntiforgery();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FaunaFinder.Client._Imports).Assembly);

app.MapStateEndpoints();
app.MapMunicipalityEndpoints();
app.MapWildlifeApiEndpoints();
app.MapFaunaFinderMcp();

app.Run();
