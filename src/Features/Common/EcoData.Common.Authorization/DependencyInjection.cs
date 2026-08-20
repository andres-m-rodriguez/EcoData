using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Authorization;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the unified <see cref="IAuthorization"/> entry point. Named to avoid
    /// colliding with ASP.NET Core's AddAuthorization, which hosts also call. Add a source per
    /// scope kind the host uses with <see cref="AddPermissionSource{T}"/>.
    /// </summary>
    public static IServiceCollection AddPermissions(this IServiceCollection services)
    {
        services.AddScoped<IAuthorization, Authorization>();

        return services;
    }

    /// <summary>
    /// Registers the source answering for one scope kind. Two sources claiming the same
    /// kind throws when <see cref="IAuthorization"/> is first resolved.
    /// </summary>
    public static IServiceCollection AddPermissionSource<T>(this IServiceCollection services)
        where T : class, IPermissionSource
    {
        services.AddScoped<IPermissionSource, T>();

        return services;
    }
}
