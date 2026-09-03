using EcoData.Common.i18n;
using EcoData.Common.Problems;
using EcoData.Locations.Application.Client;
using EcoData.Spa.Navigation;
using EcoData.Ui;
using EcoData.Wildlife.Application.Client;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.Account;
using FaunaFinder.Client.Services.Shapes;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Tempest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTempest();

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddProblemHandlers();

builder.Services.AddLocationsClient(baseAddress);
builder.Services.AddWildlifeClient(baseAddress);

builder.Services.AddHttpClient<IAccountHttpClient, AccountHttpClient>(client =>
{
    client.BaseAddress = baseAddress;
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, FaunaFinderAuthStateProvider>();
builder.Services.AddAuthorizationCore();

builder.Services.AddSpaNavigation();
builder.Services.AddEcoDataUi();
builder.Services.AddMudServices();

builder.Services.AddSingleton<ShapeAreaRequest>();

builder.Services.AddSingleton<ILocalizer>(_ => new Localizer(
    FaunaFinderStrings.Languages,
    FaunaFinderStrings.Translations));

await builder.Build().RunAsync();
