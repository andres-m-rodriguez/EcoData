using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class SensorEndpoints
{
    public static IEndpointRouteBuilder MapSensorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors");

        group
            .MapGet(
                "/",
                ([AsParameters] SensorParameters parameters, ISensorRepository repository, CancellationToken ct) =>
                    repository.GetSensorsAsync(parameters, ct)
            )
            .WithName("GetSensors");

        var orgGroup = app.MapGroup("/api/organizations/{organizationId:guid}/sensors")
            .WithTags("Organization Sensors")
            .RequireAuthorization(PolicyNames.Admin);

        orgGroup
            .MapGet(
                "/",
                async Task<Results<Ok<List<SensorDtoForList>>, NotFound<string>>> (
                    Guid organizationId,
                    ISensorRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var sensors = await repository.GetByOrganizationAsync(organizationId, ct).ToListAsync(ct);
                    return TypedResults.Ok(sensors);
                }
            )
            .WithName("GetOrganizationSensors");

        orgGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SensorDtoForDetail>, NotFound>> (
                    Guid organizationId,
                    Guid id,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await repository.GetByIdAsync(id, ct);
                    if (sensor is null || sensor.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(sensor);
                }
            )
            .WithName("GetOrganizationSensorById");

        orgGroup
            .MapPost(
                "/",
                async Task<Results<Created<SensorDtoForCreated>, NotFound<string>>> (
                    Guid organizationId,
                    SensorDtoForOrganizationCreate dto,
                    ISensorRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var created = await repository.CreateForOrganizationAsync(organizationId, dto, ct);
                    return TypedResults.Created(
                        $"/api/organizations/{organizationId}/sensors/{created.Id}",
                        created
                    );
                }
            )
            .WithName("CreateOrganizationSensor");

        orgGroup
            .MapPut(
                "/{id:guid}",
                async Task<Results<Ok<SensorDtoForDetail>, NotFound>> (
                    Guid organizationId,
                    Guid id,
                    SensorDtoForOrganizationCreate dto,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var existing = await repository.GetByIdAsync(id, ct);
                    if (existing is null || existing.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    var updated = await repository.UpdateAsync(id, dto, ct);
                    if (updated is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(updated);
                }
            )
            .WithName("UpdateOrganizationSensor");

        orgGroup
            .MapDelete(
                "/{id:guid}",
                async Task<Results<NoContent, NotFound>> (
                    Guid organizationId,
                    Guid id,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var existing = await repository.GetByIdAsync(id, ct);
                    if (existing is null || existing.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    var deleted = await repository.DeleteAsync(id, ct);
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("DeleteOrganizationSensor");

        return app;
    }
}
