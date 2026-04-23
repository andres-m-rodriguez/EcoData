using EcoData.Common.i18n;
using EcoData.Locations.Application.Client;
using EcoData.NativeUi;
using EcoData.Wildlife.Application.Client;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddLocationsClient(baseAddress);
builder.Services.AddWildlifeClient(baseAddress);

builder.Services.AddNativeUi();
builder.Services.AddMudServices();

// Localization — single Localizer instance fed from FaunaFinderStrings.
builder.Services.AddSingleton<ILocalizer>(_ => new Localizer(
    FaunaFinderStrings.Languages,
    FaunaFinderStrings.Translations));

await builder.Build().RunAsync();
