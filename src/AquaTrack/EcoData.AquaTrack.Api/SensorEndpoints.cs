using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using static EcoData.AquaTrack.Contracts.Permissions;

namespace EcoData.AquaTrack.Api;

public static class SensorEndpoints
{
    public static IEndpointRouteBuilder MapSensorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] SensorParameters parameters,
                    ISensorRepository repository,
                    CancellationToken ct
                ) => repository.GetSensorsAsync(parameters, ct)
            )
            .WithName("GetSensors");

        group
            .MapGet(
                "/count",
                (
                    [AsParameters] SensorParameters parameters,
                    ISensorRepository repository,
                    CancellationToken ct
                ) => repository.GetSensorCountAsync(parameters, ct)
            )
            .WithName("GetSensorCount");

        var orgGroup = app.MapGroup("/api/organizations/{organizationId:guid}/sensors")
            .WithTags("Organization Sensors")
            .RequireAuthorization();

        orgGroup
            .MapGet(
                "/count",
                async Task<Results<Ok<int>, NotFound<string>, ForbidHttpResult>> (
                    Guid organizationId,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IOrganizationRepository organizationRepository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(
                        organizationId,
                        ct
                    );
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Read,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var count = await repository.GetCountByOrganizationAsync(organizationId, ct);
                    return TypedResults.Ok(count);
                }
            )
            .WithName("GetOrganizationSensorCount");

        orgGroup
            .MapGet(
                "/",
                async Task<
                    Results<Ok<List<SensorDtoForList>>, NotFound<string>, ForbidHttpResult>
                > (
                    Guid organizationId,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IOrganizationRepository organizationRepository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(
                        organizationId,
                        ct
                    );
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Read,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var sensors = await repository
                        .GetByOrganizationAsync(organizationId, ct)
                        .ToListAsync(ct);
                    return TypedResults.Ok(sensors);
                }
            )
            .WithName("GetOrganizationSensors");

        orgGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SensorDtoForDetail>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Read,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

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
                async Task<
                    Results<Created<SensorDtoForCreated>, NotFound<string>, ForbidHttpResult>
                > (
                    Guid organizationId,
                    SensorDtoForOrganizationCreate dto,
                    ClaimsPrincipal user,
                    ISensorRepository sensorRepository,
                    IOrganizationRepository organizationRepository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(
                        organizationId,
                        ct
                    );
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Create,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var created = await sensorRepository.CreateForOrganizationAsync(
                        organizationId,
                        dto,
                        ct
                    );
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
                async Task<Results<Ok<SensorDtoForDetail>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    SensorDtoForOrganizationCreate dto,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Update,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

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
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Sensor.Delete,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

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
