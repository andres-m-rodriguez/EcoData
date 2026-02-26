using EcoData.AquaTrack.Application.Client;
using EcoData.AquaTrack.WebApp.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddAquaTrackClient(baseAddress);

builder.Services.AddScoped<ISensorMapManager, SensorMapManager>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
