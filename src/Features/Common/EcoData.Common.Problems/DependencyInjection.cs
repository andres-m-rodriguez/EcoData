using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Problems;

/// <summary>Service registration for the problem handlers.</summary>
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Attaches both failure handlers to every <see cref="HttpClient"/> the host builds, so no
        /// client catches transport exceptions itself. <paramref name="requestTimeout"/> replaces
        /// the 30-second default after which a request is reported as timed out.
        /// </summary>
        public IServiceCollection AddProblemHandlers(TimeSpan? requestTimeout = null)
        {
            services.AddTransient<TransportFailureHandler>();
            services.AddTransient(_ => new RequestCancelledFailureHandler { Timeout = requestTimeout ?? TimeSpan.FromSeconds(30) });
            services.ConfigureHttpClientDefaults(builder =>
                builder
                    .AddHttpMessageHandler<RequestCancelledFailureHandler>()
                    .AddHttpMessageHandler<TransportFailureHandler>());
            return services;
        }
    }
}
