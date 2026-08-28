using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Contracts.Dtos;
using FaunaFinder.Client.Layout;
using OneOf;
using Tempest;

namespace FaunaFinder.Client.Services.Account;

public sealed class AuthStateService(IAccountHttpClient accountClient, IEventBus bus)
{
    private UserInfo? _currentUser;
    private OrganizationAccessRequestDto? _accessRequest;
    private bool _isInitialized;

    public UserInfo? CurrentUser => _currentUser;
    public OrganizationAccessRequestDto? AccessRequest => _accessRequest;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _currentUser = await accountClient.GetCurrentUserAsync();
        if (_currentUser is not null)
        {
            await RefreshAccessRequestAsync();
        }

        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request
    )
    {
        var result = await accountClient.LoginAsync(request);

        if (result.IsT0)
        {
            _currentUser = result.AsT0;
            await RefreshAccessRequestAsync();
        }

        NotifyStateChanged();
        return result;
    }

    public async Task<OneOf<FaunaFinderSignupResponse, ValidationFailed, RequestFailed>> SignupAsync(
        FaunaFinderSignupRequest request
    )
    {
        var result = await accountClient.SignupAsync(request);

        if (result.IsT0)
        {
            _currentUser = result.AsT0.User;
            await RefreshAccessRequestAsync();
        }

        NotifyStateChanged();
        return result;
    }

    public async Task LogoutAsync()
    {
        // Best-effort: clear local auth state even if the server call fails —
        // the user asked to sign out and the cookie may already be gone.
        await accountClient.LogoutAsync();
        _currentUser = null;
        _accessRequest = null;
        NotifyStateChanged();
    }

    private async Task RefreshAccessRequestAsync()
    {
        var result = await accountClient.GetAccessRequestsAsync();
        _accessRequest = result.Match<OrganizationAccessRequestDto?>(
            requests => requests.OrderByDescending(r => r.CreatedAt).FirstOrDefault(),
            _ => null
        );
    }

    private void NotifyStateChanged()
    {
        bus.Publish<MainLayout.AuthChanged>();
    }
}
