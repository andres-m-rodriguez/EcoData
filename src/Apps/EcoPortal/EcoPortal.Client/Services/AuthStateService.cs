using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoPortal.Client.Layout;
using OneOf;
using Tempest;

namespace EcoPortal.Client.Services;

public sealed class AuthStateService(IAuthHttpClient authClient, IEventBus bus)
{
    private UserInfo? _currentUser;
    private bool _isInitialized;

    public UserInfo? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _isInitialized;
    public bool IsGlobalAdmin => _currentUser?.GlobalRole == GlobalRole.GlobalAdmin;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _currentUser = await authClient.GetCurrentUserAsync();
        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request
    )
    {
        var result = await authClient.LoginAsync(request);

        if (result.IsT0)
        {
            _currentUser = result.AsT0.User;
        }

        NotifyStateChanged();
        return result.Match<OneOf<UserInfo, ValidationFailed, RequestFailed>>(
            loginResponse => loginResponse.User,
            validationFailed => validationFailed,
            requestFailed => requestFailed
        );
    }

    public async Task LogoutAsync()
    {
        // Best-effort: clear local auth state even if the server call fails —
        // the user asked to sign out and the cookie may already be gone.
        await authClient.LogoutAsync();
        _currentUser = null;
        NotifyStateChanged();
    }

    public void UpdateCurrentUser(UserInfo user)
    {
        _currentUser = user;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        bus.Publish<MainLayout.AuthChanged>();
    }
}
