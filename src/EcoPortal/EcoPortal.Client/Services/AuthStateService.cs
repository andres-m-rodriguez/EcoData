using EcoData.Identity.Application.Client;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;

namespace EcoPortal.Client.Services;

public sealed class AuthStateService(IAuthHttpClient authClient)
{
    private UserInfo? _currentUser;
    private bool _isInitialized;
    private ClientAuthStateProvider? _authStateProvider;

    public UserInfo? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _isInitialized;
    public bool IsGlobalAdmin => _currentUser?.GlobalRole == GlobalRole.GlobalAdmin;

    public event Action? OnAuthStateChanged;

    public void SetAuthStateProvider(ClientAuthStateProvider provider)
    {
        _authStateProvider = provider;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _currentUser = await authClient.GetCurrentUserAsync();
        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var result = await authClient.LoginAsync(request);

        if (result.Success)
        {
            _currentUser = result.User;
        }

        NotifyStateChanged();
        return result;
    }

    public async Task LogoutAsync()
    {
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
        OnAuthStateChanged?.Invoke();
        _authStateProvider?.NotifyAuthenticationStateChanged();
    }
}
