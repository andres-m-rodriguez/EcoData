// FaunaFinder Blazor host — Aspire-orchestrated container app.
using EcoData.Common.Authorization;
using EcoData.Identity.Api.Authentication;
using EcoData.Identity.Api.Endpoints;
using EcoData.Identity.Api.RateLimiting;
using EcoData.Identity.Application.Extensions;
using EcoData.Identity.DataAccess.Extensions;
using EcoData.Identity.Database.Extensions;
using EcoData.Locations.Api;
using EcoData.Locations.DataAccess.Extensions;
using EcoData.Locations.Database.Extensions;
using EcoData.Organization.Authorization;
using EcoData.Organization.DataAccess;
using EcoData.Organization.Database.Extensions;
using EcoData.Wildlife.Api;
using EcoData.Wildlife.DataAccess;
using EcoData.Wildlife.Database.Extensions;
using FaunaFinder.Server.Authorization;
using FaunaFinder.Server.Components;
using FaunaFinder.Server.Mcp;
using FaunaFinder.Server.RateLimiting;
using MudBlazor.Services;
using Tempest;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddIdentityDatabase();
builder.AddLocationsDatabase();
builder.AddOrganizationDatabase();
builder.AddWildlifeDatabase();

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddTempest();
builder.Services.AddIdentityDataAccess();
builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddLocationsDataAccess();
builder.Services.AddOrganizationDataAccess();
builder.Services.AddWildlifeDataAccess(builder.Configuration);
builder.Services.AddFaunaFinderMcp();
builder.Services.AddFaunaFinderRateLimiting();

// FaunaFinder issues its own cookie: auth_token is host-only and SameSite=Strict, so an
// EcoPortal session never reaches this host. Same accounts, separate sign-in.
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = UserJwtAuthentication.SchemeName;
        options.DefaultChallengeScheme = UserJwtAuthentication.SchemeName;
    })
    .AddUserJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddPermissions();
builder.Services.AddOrganizationPermissionSource();
builder.Services.AddLoginRateLimiting();

builder.Services.Configure<FaunaFinderOptions>(
    builder.Configuration.GetSection(FaunaFinderOptions.SectionName)
);
builder.Services.AddSingleton<FaunaFinderOrganization>();
builder.Services.AddHostedService<FaunaFinderOrganizationResolver>();
builder.Services.AddScoped<IFaunaFinderPermission, FaunaFinderPermission>();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseLoginRateLimiting();
app.UseAntiforgery();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FaunaFinder.Client._Imports).Assembly);

app.MapUserAuthEndpoints();
app.MapStateEndpoints();
app.MapMunicipalityEndpoints();
app.MapWildlifeApiEndpoints();
app.MapFaunaFinderMcp();

app.Run();
