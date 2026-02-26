using EcoData.AquaTrack.Api;
using EcoData.AquaTrack.Database.Extensions;
using EcoData.AquaTrack.DataAccess.Extensions;
using EcoData.AquaTrack.WebApp.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAquaTrackDatabase();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
builder.Services.AddAquaTrackDataAccess();

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
    .AddAdditionalAssemblies(typeof(EcoData.AquaTrack.WebApp.Client._Imports).Assembly);

app.MapSensorEndpoints();

app.Run();
