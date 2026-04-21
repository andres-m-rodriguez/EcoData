using EcoData.Wildlife.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api;

public static class WildlifeApiExtensions
{
    public static IEndpointRouteBuilder MapWildlifeApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSpeciesEndpoints();
        app.MapSpeciesCategoryEndpoints();
        app.MapConservationEndpoints();

        return app;
    }
}
