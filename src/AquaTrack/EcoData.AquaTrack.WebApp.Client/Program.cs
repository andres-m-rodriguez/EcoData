using EcoData.AquaTrack.Application.Client;
using EcoData.AquaTrack.WebApp.Client.Services;
using EcoData.Identity.Application.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddAquaTrackClient(baseAddress);
builder.Services.AddIdentityClient(baseAddress);

builder.Services.AddScoped<ISensorMapManager, SensorMapManager>();
builder.Services.AddScoped<BackButtonService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ClientAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ClientAuthStateProvider>()
);
builder.Services.AddIdentityAuthorization();

builder.Services.AddMudServices();

await builder.Build().RunAsync();
