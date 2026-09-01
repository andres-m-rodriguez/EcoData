using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using FaunaFinder.Server.Account;
using FaunaFinder.Server.Components;
using FaunaFinder.Server.Mcp;
using FaunaFinder.Server.Organization;
using FaunaFinder.Server.RateLimiting;
using FaunaFinder.Server.Reports;
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

builder.Services.AddHttpClient(
    FaunaFinderOrganizationLoader.HttpClientName,
    client => client.BaseAddress = new Uri("https+http://ecoportal")
);

// Account proxying needs its own client: the default handler keeps a shared
// CookieContainer, which would capture one user's Set-Cookie and replay it on
// every later request through the same handler.
builder
    .Services.AddHttpClient(
        AccountEndpoints.HttpClientName,
        client => client.BaseAddress = new Uri("https+http://ecoportal")
    )
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false });
builder.Services.AddSingleton<FaunaFinderOrganizationLoader>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FaunaFinderOrganizationLoader>());
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<FaunaFinderOrganizationLoader>().Current
    ?? throw new InvalidOperationException("The FaunaFinder organization has not been resolved.")
);

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
app.MapSpeciesReportEndpoints();
app.MapAccountEndpoints();
app.MapFaunaFinderMcp();

app.Run();
