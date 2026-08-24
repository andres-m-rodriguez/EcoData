using EcoData.Locations.Mcp;
using EcoData.Wildlife.Mcp;

namespace FaunaFinder.Server.Mcp;

public static class FaunaFinderMcpExtensions
{
    public static IServiceCollection AddFaunaFinderMcp(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport()
            .AddWildlifeMcpTools()
            .AddLocationsMcpTools();

        return services;
    }

    public static IEndpointRouteBuilder MapFaunaFinderMcp(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
