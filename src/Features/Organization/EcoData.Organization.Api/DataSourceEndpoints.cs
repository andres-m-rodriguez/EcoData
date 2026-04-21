using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Organization.Api;

public static class DataSourceEndpoints
{
    public static IEndpointRouteBuilder MapDataSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organization/datasources").WithTags("DataSources");

        group
            .MapGet(
                "/",
                (IDataSourceRepository repository, CancellationToken ct) =>
                    repository.GetAllAsync(ct)
            )
            .WithName("GetDataSources");

        return app;
    }
}
