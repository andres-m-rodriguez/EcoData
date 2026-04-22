using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoPortal.Client.Services;

public sealed class AuthStateService(IAuthHttpClient authClient)
{
    private UserInfo? _currentUser;
    private bool _isInitialized;

    public UserInfo? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _isInitialized;
    public bool IsGlobalAdmin => _currentUser?.GlobalRole == GlobalRole.GlobalAdmin;

    public event Action? OnAuthStateChanged;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _currentUser = await authClient.GetCurrentUserAsync();
        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<OneOf<UserInfo, ProblemDetail>> LoginAsync(LoginRequest request)
    {
        var result = await authClient.LoginAsync(request);

        if (result.IsT0)
        {
            _currentUser = result.AsT0.User;
        }

        NotifyStateChanged();
        return result.Match<OneOf<UserInfo, ProblemDetail>>(
            loginResponse => loginResponse.User,
            problemDetail => problemDetail
        );
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
    }
}
