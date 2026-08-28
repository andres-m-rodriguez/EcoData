using EcoData.Identity.Contracts.Claims;
using FaunaFinder.Client.Layout;
using Microsoft.AspNetCore.Components.Authorization;
using Tempest;

namespace FaunaFinder.Client.Services.Account;

// Bridges AuthStateService into Blazor's authentication system. Not a Tempest
// component, so it can't use [Event]; it subscribes to the bus directly and
// disposes the subscription with the scope.
public sealed class FaunaFinderAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthStateService _authStateService;
    private readonly IDisposable _subscription;

    public FaunaFinderAuthStateProvider(AuthStateService authStateService, IEventBus bus)
    {
        _authStateService = authStateService;
        _subscription = bus.Subscribe(
            typeof(MainLayout.AuthChanged),
            _ =>
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return Task.CompletedTask;
            }
        );
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_authStateService.IsInitialized)
        {
            await _authStateService.InitializeAsync();
        }

        var principal = _authStateService.CurrentUser.ToClaimsPrincipal();
        return new AuthenticationState(principal);
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
