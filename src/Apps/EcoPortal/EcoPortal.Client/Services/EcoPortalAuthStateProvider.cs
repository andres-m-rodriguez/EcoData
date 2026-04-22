using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EcoPortal.Client.Services;

public sealed class EcoPortalAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthStateService _authStateService;

    public EcoPortalAuthStateProvider(AuthStateService authStateService)
    {
        _authStateService = authStateService;
        _authStateService.OnAuthStateChanged += HandleAuthStateChanged;
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

    private void HandleAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _authStateService.OnAuthStateChanged -= HandleAuthStateChanged;
    }
}
