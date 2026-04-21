using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api;

public static class WildlifeApiExtensions
{
    public static IEndpointRouteBuilder MapWildlifeApiEndpoints(this IEndpointRouteBuilder app)
    {
        // Endpoints will be mapped here as they are implemented
        // Example:
        // app.MapSpeciesEndpoints();
        // app.MapSightingEndpoints();

        return app;
    }
}
