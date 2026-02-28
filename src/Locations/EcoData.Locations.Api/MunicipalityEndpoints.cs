using System.Text.Json;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Locations.Api;

public static class MunicipalityEndpoints
{
    public static IEndpointRouteBuilder MapMunicipalityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/municipalities").WithTags("Municipalities");

        group.MapGet("/", GetMunicipalities).WithName("GetMunicipalities");
        group.MapGet("/{id:guid}", GetMunicipalityById).WithName("GetMunicipalityById");
        group
            .MapGet("/geojson-id/{geoJsonId}", GetMunicipalityByGeoJsonId)
            .WithName("GetMunicipalityByGeoJsonId");
        group.MapGet("/by-point", GetMunicipalityByPoint).WithName("GetMunicipalityByPoint");
        group
            .MapGet("/geojson/state/{stateCode}", GetGeoJsonByStateCode)
            .WithName("GetMunicipalitiesGeoJsonByStateCode");

        return app;
    }

    private static IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalities(
        [AsParameters] MunicipalityParameters parameters,
        IMunicipalityRepository repository,
        CancellationToken ct
    ) => repository.GetMunicipalitiesAsync(parameters, ct);

    private static async Task<IResult> GetMunicipalityById(
        Guid id,
        IMunicipalityRepository repository,
        CancellationToken ct
    )
    {
        var municipality = await repository.GetByIdAsync(id, ct);
        return municipality is not null ? Results.Ok(municipality) : Results.NotFound();
    }

    private static async Task<IResult> GetMunicipalityByGeoJsonId(
        string geoJsonId,
        IMunicipalityRepository repository,
        CancellationToken ct
    )
    {
        var municipality = await repository.GetByGeoJsonIdAsync(geoJsonId, ct);
        return municipality is not null ? Results.Ok(municipality) : Results.NotFound();
    }

    private static async Task<IResult> GetMunicipalityByPoint(
        decimal latitude,
        decimal longitude,
        IMunicipalityRepository repository,
        CancellationToken ct
    )
    {
        var municipality = await repository.GetByPointAsync(latitude, longitude, ct);
        return municipality is not null ? Results.Ok(municipality) : Results.NotFound();
    }

    private static async Task<IResult> GetGeoJsonByStateCode(
        string stateCode,
        IMunicipalityRepository repository,
        CancellationToken ct
    )
    {
        var municipalities = await repository.GetGeoJsonByStateCodeAsync(stateCode, ct);

        if (municipalities.Count == 0)
        {
            return Results.NotFound();
        }

        var features = municipalities
            .Where(m => m.BoundaryGeoJson is not null)
            .Select(m => new
            {
                type = "Feature",
                id = m.GeoJsonId,
                properties = new
                {
                    id = m.Id,
                    name = m.Name,
                    geoJsonId = m.GeoJsonId,
                    centroidLatitude = m.CentroidLatitude,
                    centroidLongitude = m.CentroidLongitude,
                },
                geometry = JsonSerializer.Deserialize<JsonElement>(m.BoundaryGeoJson!),
            });

        var featureCollection = new { type = "FeatureCollection", features = features };

        return Results.Json(featureCollection);
    }
}
