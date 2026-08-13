using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace FaunaFinder.Server.RateLimiting;

/// <summary>
/// Caps how hard one caller can hit FaunaFinder's data endpoints.
///
/// <para>Everything the app serves is anonymous, so there is no user to key on
/// and the partition is the client address. Two buckets rather than one: the
/// MCP connector is looped over by a model that pages until it has what it
/// wants, while the REST endpoints are paced by a person clicking, so they are
/// not the same kind of traffic and should not share a budget.</para>
///
/// <para>A token bucket rather than a window: both callers arrive in bursts —
/// a page view fires several requests at once, and a model fans out tool calls
/// — and a bucket absorbs that up to its capacity while still holding the
/// long-run rate down. A window of the same size would reject the burst and
/// then sit idle.</para>
///
/// <para>Only the data paths are metered. A Blazor WebAssembly load pulls down
/// a long tail of static assets, and a limiter that counted those would reject
/// the app while it was still booting.</para>
/// </summary>
public static class FaunaFinderRateLimiterExtensions
{
    /// <summary>The connector: a model pages through tools without a human pacing it.</summary>
    private const int McpRequestsPerMinute = 60;

    /// <summary>The REST endpoints: several calls per page view, driven by a reader.</summary>
    private const int ApiRequestsPerMinute = 120;

    /// <summary>
    /// How often the bucket tops up. Short enough that a caller who runs dry
    /// waits seconds rather than a minute, which matters most for the
    /// connector: a model that stalls a full minute tends to give up on the
    /// tool rather than wait for it.
    /// </summary>
    private static readonly TimeSpan ReplenishmentPeriod = TimeSpan.FromSeconds(10);

    private const int PeriodsPerMinute = 6;

    private const string McpPath = "/mcp";
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
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path;

                if (path.StartsWithSegments(McpPath, StringComparison.OrdinalIgnoreCase))
                {
                    return TokenBucket($"mcp:{ClientKey(context)}", McpRequestsPerMinute);
                }

                foreach (var apiPath in ApiPaths)
                {
                    if (path.StartsWithSegments(apiPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return TokenBucket($"api:{ClientKey(context)}", ApiRequestsPerMinute);
                    }
                }

                // Pages, static assets and the WASM payload: unmetered.
                return RateLimitPartition.GetNoLimiter("unmetered");
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Tell the caller when to come back. An MCP client reads this
                // and waits; without it the usual answer is to retry at once.
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

    /// <summary>
    /// A bucket that holds a minute's worth of requests and refills a sixth of
    /// it every ten seconds — so a caller may spend the whole minute at once,
    /// then continues at the sustained rate.
    /// </summary>
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

    /// <summary>
    /// Who to count against. Callers behind one NAT share a bucket — the cost
    /// of having no identity to key on — so the buckets are sized wide enough
    /// that ordinary shared use does not trip them.
    /// </summary>
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
