using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace FaunaFinder.Server.RateLimiting;

public static class FaunaFinderRateLimiterExtensions
{
    private const int McpRequestsPerMinute = 60;

    private const int ApiRequestsPerMinute = 120;

    // Tight on purpose: everything under /account is a credential operation
    // proxied to EcoPortal, and a burst here is either a bug or an attack.
    private const int AccountRequestsPerMinute = 12;

    // Each sighting photo upload writes to storage, so the bucket is a fraction
    // of the general API one.
    private const int ImageUploadRequestsPerMinute = 24;

    private static readonly TimeSpan ReplenishmentPeriod = TimeSpan.FromSeconds(10);

    private const int PeriodsPerMinute = 6;

    private const string McpPath = "/mcp";
    private const string AccountPath = "/account";
    private const string SightingsPath = "/wildlife/sightings";
    private const string ImagesSegment = "/images";
    private static readonly string[] ApiPaths = ["/wildlife", "/locations"];

    public static IServiceCollection AddFaunaFinderRateLimiting(this IServiceCollection services)
    {
        // Container Apps terminates ingress, so the socket address is the
        // ingress' own. Without this every caller collapses into one partition
        // and the limiter throttles the whole world together instead of per
        // client. The proxy is upstream and unknown to us by address, so the
        // default known-network allowlist has to be cleared for the header to
        // be honoured.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path;

                if (path.StartsWithSegments(McpPath, StringComparison.OrdinalIgnoreCase))
                    return TokenBucket($"mcp:{ClientKey(context)}", McpRequestsPerMinute);

                if (path.StartsWithSegments(AccountPath, StringComparison.OrdinalIgnoreCase))
                    return TokenBucket($"account:{ClientKey(context)}", AccountRequestsPerMinute);

                if (
                    HttpMethods.IsPost(context.Request.Method)
                    && path.StartsWithSegments(SightingsPath, StringComparison.OrdinalIgnoreCase)
                    && path.Value!.EndsWith(ImagesSegment, StringComparison.OrdinalIgnoreCase)
                )
                    return TokenBucket($"images:{ClientKey(context)}", ImageUploadRequestsPerMinute);

                foreach (var apiPath in ApiPaths)
                {
                    if (path.StartsWithSegments(apiPath, StringComparison.OrdinalIgnoreCase))
                        return TokenBucket($"api:{ClientKey(context)}", ApiRequestsPerMinute);
                }

                return RateLimitPartition.GetNoLimiter("unmetered");
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // The bucket reports the wait itself; the replenishment period
                // is the floor for the shapes that don't.
                var retryAfterSeconds = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfter
                )
                    ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                    : (int)ReplenishmentPeriod.TotalSeconds;

                context.HttpContext.Response.Headers.RetryAfter =
                    retryAfterSeconds.ToString(NumberFormatInfo.InvariantInfo);

                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Slow down and try again shortly.",
                    cancellationToken
                );
            };
        });

        return services;
    }

    private static RateLimitPartition<string> TokenBucket(string key, int requestsPerMinute) =>
        RateLimitPartition.GetTokenBucketLimiter(
            key,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = requestsPerMinute,
                TokensPerPeriod = requestsPerMinute / PeriodsPerMinute,
                ReplenishmentPeriod = ReplenishmentPeriod,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,

                // Reject rather than hold the request. A queued MCP call reads
                // as a slow tool to the model, which is worse than a 429 it can
                // act on.
                QueueLimit = 0,
            }
        );

    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
