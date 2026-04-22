using System.Security.Claims;
using EcoData.Common.Messaging.Abstractions;
using EcoData.Identity.Contracts.Claims;
using EcoData.Sensors.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Events;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class UserNotificationEndpoints
{
    public static IEndpointRouteBuilder MapUserNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/me/notifications")
            .WithTags("User Notifications")
            .RequireAuthorization();

        group
            .MapGet(
                "/",
                async Task<Results<Ok<IReadOnlyList<UserNotificationDto>>, UnauthorizedHttpResult>> (
                    ClaimsPrincipal user,
                    IUserNotificationRepository repository,
                    int pageSize = 20,
                    Guid? cursor = null,
                    CancellationToken ct = default
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var notifications = await repository.GetByUserAsync(
                        token.UserId.Value,
                        Math.Min(pageSize, 100),
                        cursor,
                        ct
                    );

                    return TypedResults.Ok(notifications);
                }
            )
            .WithName("GetUserNotifications");

        group
            .MapGet(
                "/unread-count",
                async Task<Results<Ok<UnreadCountDto>, UnauthorizedHttpResult>> (
                    ClaimsPrincipal user,
                    IUserNotificationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var count = await repository.GetUnreadCountAsync(token.UserId.Value, ct);
                    return TypedResults.Ok(new UnreadCountDto(count));
                }
            )
            .WithName("GetUnreadNotificationCount");

        group
            .MapPost(
                "/{id:guid}/read",
                async Task<Results<Ok<UserNotificationDto>, NotFound, UnauthorizedHttpResult>> (
                    Guid id,
                    ClaimsPrincipal user,
                    IUserNotificationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var notification = await repository.MarkAsReadAsync(
                        token.UserId.Value,
                        id,
                        ct
                    );

                    if (notification is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(notification);
                }
            )
            .WithName("MarkNotificationAsRead");

        group
            .MapPost(
                "/read-all",
                async Task<Results<Ok<int>, UnauthorizedHttpResult>> (
                    ClaimsPrincipal user,
                    IUserNotificationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var count = await repository.MarkAllAsReadAsync(token.UserId.Value, ct);
                    return TypedResults.Ok(count);
                }
            )
            .WithName("MarkAllNotificationsAsRead");

        group
            .MapGet(
                "/stream",
                (
                    ClaimsPrincipal user,
                    IMessageBus messageBus,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return Results.Unauthorized();
                    }

                    var topic = MessageTopics.GetUserNotificationsTopic(token.UserId.Value);

                    return TypedResults.ServerSentEvents(
                        messageBus.SubscribeToEventsAsync<UserNotificationEvent>(topic, ct),
                        eventType: SseEventTypes.UserNotification
                    );
                }
            )
            .WithName("StreamUserNotifications");

        return app;
    }
}
