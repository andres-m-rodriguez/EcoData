using EcoData.Locations.Application.Client;
using EcoData.NativeUi;
using EcoData.Wildlife.Application.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddLocationsClient(baseAddress);
builder.Services.AddWildlifeClient(baseAddress);

builder.Services.AddNativeUi();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
