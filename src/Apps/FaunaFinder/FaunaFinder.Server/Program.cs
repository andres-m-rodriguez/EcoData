using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using FaunaFinder.Server.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddLocationsDataAccess();
builder.Services.AddWildlifeDataAccess(builder.Configuration);

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FaunaFinder.Client._Imports).Assembly);

app.MapStateEndpoints();
app.MapMunicipalityEndpoints();
app.MapWildlifeApiEndpoints();

app.Run();
