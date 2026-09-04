using EcoData.Common.Authorization;
using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using FaunaFinder.Server.Account;
using FaunaFinder.Server.Authentication;
using FaunaFinder.Server.Authorization;
using FaunaFinder.Server.Components;
using FaunaFinder.Server.Mcp;
using FaunaFinder.Server.Organization;
using FaunaFinder.Server.RateLimiting;
using FaunaFinder.Server.Reports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using Tempest;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddLocationsDatabase();
builder.AddWildlifeDatabase();
builder.AddAzureBlobContainerClient("sighting-images");

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddTempest();
builder.Services.AddLocationsDataAccess();
builder.Services.AddWildlifeDataAccess();
builder.Services.AddFaunaFinderMcp();
builder.Services.AddFaunaFinderRateLimiting();

builder.Services.AddHttpClient(
    FaunaFinderOrganizationLoader.HttpClientName,
    client => client.BaseAddress = new Uri("https+http://ecoportal")
);

// Account proxying needs its own client: the default handler keeps a shared
// CookieContainer, which would capture one user's Set-Cookie and replay it on
// every later request through the same handler.
// Every call through this client forwards one person's action, so nothing is
// retried. The pipeline service defaults put on every client treats the login
// limiter's 429 as transient and honours its Retry-After, which left the caller
// waiting out the whole request timeout instead of reading the limiter's
// message; a retried registration could also double-submit. That pipeline is
// one nameless handler shared by all clients, so this client drops it and
// carries its own, with the retry switched off.
builder
    .Services.AddHttpClient(
        AccountEndpoints.HttpClientName,
        client => client.BaseAddress = new Uri("https+http://ecoportal")
    )
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false })
    .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
    {
        for (var i = handlers.Count - 1; i >= 0; i--)
        {
            if (handlers[i] is ResilienceHandler)
                handlers.RemoveAt(i);
        }
    })
    .AddStandardResilienceHandler(options =>
        options.Retry.ShouldHandle = _ => ValueTask.FromResult(false));

// FaunaFinder holds no JWT secrets: the session is validated by asking
// EcoPortal, and permission questions are answered the same way.
builder
    .Services.AddAuthentication(EcoPortalSessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, EcoPortalSessionAuthenticationHandler>(
        EcoPortalSessionAuthenticationHandler.SchemeName,
        _ => { }
    );
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddPermissions();
builder.Services.AddScoped<IOrganizationPermissionSource, EcoPortalOrganizationPermissionSource>();

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
    app.UseWebAssemblyDebugging();
else
    app.UseHsts();

// The limiter runs first so an anonymous flood never reaches EcoPortal;
// antiforgery follows authentication because its tokens are bound to the
// identity.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

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
