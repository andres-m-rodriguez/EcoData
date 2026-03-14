using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.Api.RateLimiting;

public static class LoginRateLimiterExtensions
{
    public const string LoginRateLimiterPolicy = "LoginRateLimiter";
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(2);
    private const int MaxAttempts = 3;

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginRateLimiterPolicy, context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Try to get email from the request body
                // For login requests, we partition by IP + email combination
                var partitionKey = ipAddress;

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = MaxAttempts,
                        Window = CooldownPeriod,
                        SegmentsPerWindow = 3,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                );
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue
                    : CooldownPeriod;

                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Too many login attempts",
                    message = $"Too many login attempts. Please try again in {(int)retryAfter.TotalMinutes} minutes.",
                    retryAfterSeconds = (int)retryAfter.TotalSeconds
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseLoginRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}
