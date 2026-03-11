using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EcoData.AquaTrack.WebApp.Client.Services;

public sealed class ClientAuthStateProvider(AuthStateService authStateService)
    : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!authStateService.IsInitialized)
        {
            await authStateService.InitializeAsync();
        }

        var user = authStateService.CurrentUser;

        if (user is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
