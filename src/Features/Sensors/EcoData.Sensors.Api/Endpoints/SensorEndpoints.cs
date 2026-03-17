using System.Security.Claims;
using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Sensors.Contracts.Permissions;

namespace EcoData.Sensors.Api.Endpoints;

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

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SensorDtoForDetail>, NotFound>> (
                    Guid id,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await repository.GetByIdAsync(id, ct);
                    return sensor is null ? TypedResults.NotFound() : TypedResults.Ok(sensor);
                }
            )
            .WithName("GetSensorById");

        group
            .MapPost(
                "/register",
                async Task<
                    Results<
                        Ok<SensorDtoForRegistered>,
                        ValidationProblem,
                        UnauthorizedHttpResult,
                        ForbidHttpResult,
                        Conflict<string>
                    >
                > (
                    RegisterSensorRequest request,
                    ClaimsPrincipal user,
                    IOrganizationPermissionService permissionService,
                    ISensorIdentityProviderService identityProvider,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var validation = new RegisterSensorRequestValidator().Validate(request);
                    if (!validation.IsValid)
                    {
                        var errors = validation
                            .Errors.GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                        return TypedResults.ValidationProblem(errors);
                    }

                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var hasPermission = await permissionService.HasPermissionAsync(
                        token.UserId!.Value,
                        request.OrganizationId,
                        Permissions.Sensor.Create,
                        ct
                    );

                    if (!hasPermission)
                    {
                        return TypedResults.Forbid();
                    }

                    var result = await repository.RegisterAsync(request, ct);

                    if (result.IsT1)
                    {
                        return TypedResults.Conflict(result.AsT1.Message);
                    }

                    var sensorId = result.AsT0;

                    var credentials = await identityProvider.ProvisionAsync(
                        sensorId,
                        request.OrganizationId,
                        request.OrganizationName,
                        request.Name,
                        ct
                    );

                    return TypedResults.Ok(
                        new SensorDtoForRegistered(
                            credentials.SensorId,
                            credentials.AccessToken,
                            credentials.ExpiresAt
                        )
                    );
                }
            )
            .RequireAuthorization()
            .WithName("RegisterSensor");

        group
            .MapPut(
                "/{id:guid}",
                async Task<
                    Results<
                        Ok<SensorDtoForDetail>,
                        ValidationProblem,
                        NotFound,
                        UnauthorizedHttpResult,
                        ForbidHttpResult
                    >
                > (
                    Guid id,
                    SensorDtoForUpdate request,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var validation = new SensorDtoForUpdateValidator().Validate(request);
                    if (!validation.IsValid)
                    {
                        var errors = validation
                            .Errors.GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                        return TypedResults.ValidationProblem(errors);
                    }

                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var existing = await repository.GetByIdAsync(id, ct);
                    if (existing is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var hasPermission = await permissionService.HasPermissionAsync(
                        token.UserId!.Value,
                        existing.OrganizationId,
                        Permissions.Sensor.Update,
                        ct
                    );

                    if (!hasPermission)
                    {
                        return TypedResults.Forbid();
                    }

                    var updated = await repository.UpdateAsync(id, request, ct);
                    return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
                }
            )
            .RequireAuthorization()
            .WithName("UpdateSensor");

        group
            .MapDelete(
                "/{id:guid}",
                async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ForbidHttpResult>> (
                    Guid id,
                    ClaimsPrincipal user,
                    ISensorRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var existing = await repository.GetByIdAsync(id, ct);
                    if (existing is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var hasPermission = await permissionService.HasPermissionAsync(
                        token.UserId!.Value,
                        existing.OrganizationId,
                        Permissions.Sensor.Delete,
                        ct
                    );

                    if (!hasPermission)
                    {
                        return TypedResults.Forbid();
                    }

                    var deleted = await repository.DeleteAsync(id, ct);
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .RequireAuthorization()
            .WithName("DeleteSensor");

        return app;
    }
}
