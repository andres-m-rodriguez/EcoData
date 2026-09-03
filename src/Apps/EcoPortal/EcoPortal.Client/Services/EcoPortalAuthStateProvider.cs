using EcoData.Identity.Contracts.Claims;
using EcoPortal.Client.Layout;
using Microsoft.AspNetCore.Components.Authorization;
using Tempest;

namespace EcoPortal.Client.Services;

// Bridges AuthStateService into Blazor's authentication system. Not a Tempest
// component, so it can't use [Event]; it subscribes to the bus directly and
// disposes the subscription with the scope.
public sealed class EcoPortalAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthStateService _authStateService;
    private readonly IDisposable _subscription;

    public EcoPortalAuthStateProvider(AuthStateService authStateService, IEventBus bus)
    {
        _authStateService = authStateService;
        _subscription = bus.Subscribe(
            typeof(MainLayout.AuthChanged),
            _ =>
            {
                var authenticationState = GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(authenticationState);
                return Task.CompletedTask;
            }
        );
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_authStateService.IsInitialized)
            await _authStateService.InitializeAsync();

        var principal = _authStateService.CurrentUser.ToClaimsPrincipal();
        return new AuthenticationState(principal);
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
