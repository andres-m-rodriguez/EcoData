using EcoData.Common.i18n;
using EcoData.Locations.Application.Client;
using EcoData.Spa.Navigation;
using EcoData.Wildlife.Application.Client;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.FieldNotebook;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Tempest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTempest();

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddLocationsClient(baseAddress);
builder.Services.AddWildlifeClient(baseAddress);

// The four bottom-nav destinations. Everything else — including Browse's
// single-segment children — is a page you can go back from.
builder.Services.AddSpaNavigation("/", "/species", "/municipalities", "/browse");
builder.Services.AddMudServices();

// The reader's browser-local state. Singleton, not scoped: one browser, and the
// library rail has to see writes made by a page.
builder.Services.AddSingleton<IFieldNotebook, FieldNotebook>();

// Localization — single Localizer instance fed from FaunaFinderStrings.
builder.Services.AddSingleton<ILocalizer>(_ => new Localizer(
    FaunaFinderStrings.Languages,
    FaunaFinderStrings.Translations));

await builder.Build().RunAsync();
