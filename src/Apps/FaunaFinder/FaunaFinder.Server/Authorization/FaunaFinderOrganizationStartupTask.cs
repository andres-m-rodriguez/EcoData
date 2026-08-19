namespace FaunaFinder.Server.Authorization;

/// <summary>
/// The host's choice of <em>when</em>: resolve once, before the app serves traffic.
/// </summary>
/// <remarks>
/// Swapping this for lazy resolution or a periodic refresh means replacing this class and
/// nothing else — <see cref="FaunaFinderOrganizationResolver"/> has no lifecycle of its own.
/// </remarks>
public sealed class FaunaFinderOrganizationStartupTask(
    IServiceScopeFactory scopeFactory,
    FaunaFinderOrganizationAccessor accessor
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var resolver =
            scope.ServiceProvider.GetRequiredService<FaunaFinderOrganizationResolver>();

        accessor.Organization = await resolver.ResolveAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
