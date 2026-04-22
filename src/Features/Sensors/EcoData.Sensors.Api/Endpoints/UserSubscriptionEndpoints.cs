using System.Security.Claims;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class UserSubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapUserSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var userGroup = app.MapGroup("/users/me/subscriptions")
            .WithTags("User Subscriptions")
            .RequireAuthorization();

        userGroup
            .MapGet(
                "/",
                (
                    ClaimsPrincipal user,
                    IUserSensorSubscriptionRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok(repository.GetByUserAsync(token.UserId.Value, ct));
                }
            )
            .WithName("GetUserSubscriptions");

        var sensorGroup = app.MapGroup("/sensors/{sensorId:guid}/subscribe")
            .WithTags("User Subscriptions")
            .RequireAuthorization();

        sensorGroup
            .MapGet(
                "/",
                async Task<Results<Ok<UserSensorSubscriptionDto>, NotFound, UnauthorizedHttpResult>> (
                    Guid sensorId,
                    ClaimsPrincipal user,
                    IUserSensorSubscriptionRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var subscription = await repository.GetAsync(token.UserId.Value, sensorId, ct);
                    if (subscription is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(subscription);
                }
            )
            .WithName("GetSensorSubscription");

        sensorGroup
            .MapPost(
                "/",
                async Task<
                    Results<
                        Created<UserSensorSubscriptionDto>,
                        ProblemHttpResult,
                        UnauthorizedHttpResult,
                        ForbidHttpResult
                    >
                > (
                    Guid sensorId,
                    UserSensorSubscriptionDtoForCreate request,
                    ClaimsPrincipal user,
                    IUserSensorSubscriptionRepository subscriptionRepository,
                    ISensorRepository sensorRepository,
                    ISensorHealthRepository healthRepository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.Problem(
                            detail: "Sensor not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    // Check if user has access to the organization (using read permission to verify membership)
                    var hasAccess = await permissionService.HasPermissionAsync(
                        token.UserId.Value,
                        sensor.OrganizationId,
                        Contracts.Permissions.Sensor.Read,
                        ct
                    );

                    if (!hasAccess)
                    {
                        return TypedResults.Forbid();
                    }

                    // Check if sensor has health monitoring enabled
                    var healthConfig = await healthRepository.GetConfigByIdAsync(sensorId, ct);
                    if (healthConfig is null || !healthConfig.IsMonitoringEnabled)
                    {
                        return TypedResults.Problem(
                            detail: "Health monitoring is not enabled for this sensor.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }

                    // Check if already subscribed
                    var existing = await subscriptionRepository.ExistsAsync(
                        token.UserId.Value,
                        sensorId,
                        ct
                    );
                    if (existing)
                    {
                        return TypedResults.Problem(
                            detail: "Already subscribed to this sensor.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var subscription = await subscriptionRepository.CreateAsync(
                        token.UserId.Value,
                        sensorId,
                        request,
                        ct
                    );

                    return TypedResults.Created(
                        $"/sensors/{sensorId}/subscribe",
                        subscription
                    );
                }
            )
            .WithName("SubscribeToSensor");

        sensorGroup
            .MapPatch(
                "/",
                async Task<
                    Results<Ok<UserSensorSubscriptionDto>, NotFound, UnauthorizedHttpResult>
                > (
                    Guid sensorId,
                    UserSensorSubscriptionDtoForUpdate request,
                    ClaimsPrincipal user,
                    IUserSensorSubscriptionRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var subscription = await repository.UpdateAsync(
                        token.UserId.Value,
                        sensorId,
                        request,
                        ct
                    );

                    if (subscription is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(subscription);
                }
            )
            .WithName("UpdateSensorSubscription");

        sensorGroup
            .MapDelete(
                "/",
                async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> (
                    Guid sensorId,
                    ClaimsPrincipal user,
                    IUserSensorSubscriptionRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var deleted = await repository.DeleteAsync(token.UserId.Value, sensorId, ct);
                    if (!deleted)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.NoContent();
                }
            )
            .WithName("UnsubscribeFromSensor");

        return app;
    }
}
