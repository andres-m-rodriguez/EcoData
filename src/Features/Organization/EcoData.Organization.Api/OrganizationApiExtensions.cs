using Microsoft.AspNetCore.Routing;

namespace EcoData.Organization.Api;

public static class OrganizationApiExtensions
{
    public static IEndpointRouteBuilder MapOrganizationApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapOrganizationEndpoints();
        app.MapOrganizationAccessRequestEndpoints();
        app.MapOrganizationBlockedUserEndpoints();
        app.MapMemberEndpoints();
        app.MapPermissionEndpoints();
        app.MapDataSourceEndpoints();

        return app;
    }
}
