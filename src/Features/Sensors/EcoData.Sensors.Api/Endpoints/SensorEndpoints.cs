using System.Security.Claims;
using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
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
                        Ok<SensorRegistrationResultDto>,
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
                    IDbContextFactory<SensorsDbContext> contextFactory,
                    CancellationToken ct
                ) =>
                {
                    var validation = new RegisterSensorRequestValidator().Validate(request);
                    if (!validation.IsValid)
                    {
                        var errors = validation.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                        return TypedResults.ValidationProblem(errors);
                    }

                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var userId = token.UserId!.Value;

                    var hasPermission = await permissionService.HasPermissionAsync(
                        userId,
                        request.OrganizationId,
                        Permissions.Sensor.Create,
                        ct
                    );

                    if (!hasPermission)
                    {
                        return TypedResults.Forbid();
                    }

                    await using var context = await contextFactory.CreateDbContextAsync(ct);

                    var existingSensor = await context.Sensors.AnyAsync(
                        s =>
                            s.OrganizationId == request.OrganizationId
                            && s.ExternalId == request.ExternalId,
                        ct
                    );

                    if (existingSensor)
                    {
                        return TypedResults.Conflict(
                            $"A sensor with external ID '{request.ExternalId}' already exists in this organization."
                        );
                    }

                    var sensorId = Guid.CreateVersion7();
                    var now = DateTimeOffset.UtcNow;

                    var sensor = new Sensor
                    {
                        Id = sensorId,
                        OrganizationId = request.OrganizationId,
                        SourceId = null,
                        ExternalId = request.ExternalId,
                        Name = request.Name,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        MunicipalityId = request.MunicipalityId,
                        IsActive = true,
                        ReportingMode = ReportingMode.Push,
                        SensorTypeId = request.SensorTypeId,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };

                    context.Sensors.Add(sensor);
                    await context.SaveChangesAsync(ct);

                    var credentials = await identityProvider.ProvisionAsync(
                        sensorId,
                        request.OrganizationId,
                        request.OrganizationName,
                        request.Name,
                        ct
                    );

                    return TypedResults.Ok(
                        new SensorRegistrationResultDto(
                            credentials.SensorId,
                            credentials.AccessToken,
                            credentials.ExpiresAt
                        )
                    );
                }
            )
            .RequireAuthorization()
            .WithName("RegisterSensor");

        return app;
    }
}
