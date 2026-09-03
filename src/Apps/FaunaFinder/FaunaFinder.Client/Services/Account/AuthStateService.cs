using EcoData.Common.Authorization;
using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Wildlife.Contracts;
using FaunaFinder.Client.Layout;
using OneOf;
using Tempest;

namespace FaunaFinder.Client.Services.Account;

public sealed class AuthStateService(IAccountHttpClient accountClient, IEventBus bus)
{
    private UserInfo? _currentUser;
    private FaunaFinderOrganizationDto? _organization;
    private OrganizationAccessRequestDto? _accessRequest;
    private OrganizationGrants _grants = OrganizationGrants.None;
    private bool _isInitialized;

    public UserInfo? CurrentUser => _currentUser;
    public FaunaFinderOrganizationDto? Organization => _organization;
    public OrganizationAccessRequestDto? AccessRequest => _accessRequest;
    public OrganizationGrants Grants => _grants;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _isInitialized;

    // Shapes navigation only; the sighting endpoints enforce the permission.
    public bool CanReviewSightings =>
        _grants.IsGlobalAdmin || _grants.Permissions.Contains(Permissions.Occurrence.Verify);

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        var organization = await accountClient.GetOrganizationAsync();
        _organization = organization.Match<FaunaFinderOrganizationDto?>(o => o, _ => null);

        _currentUser = await accountClient.GetCurrentUserAsync();
        if (_currentUser is not null)
            await RefreshMembershipAsync();

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
            await RefreshMembershipAsync();
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
            await RefreshMembershipAsync();
        }

        NotifyStateChanged();
        return result;
    }

    public async Task LogoutAsync()
    {
        // Best-effort: clear local auth state even if the server call fails,
        // the user asked to sign out and the cookie may already be gone.
        await accountClient.LogoutAsync();
        _currentUser = null;
        _accessRequest = null;
        _grants = OrganizationGrants.None;
        NotifyStateChanged();
    }

    private async Task RefreshMembershipAsync()
    {
        var requests = await accountClient.GetAccessRequestsAsync();
        _accessRequest = requests.Match<OrganizationAccessRequestDto?>(
            r => r.OrderByDescending(x => x.CreatedAt).FirstOrDefault(),
            _ => null
        );

        var permissions = await accountClient.GetPermissionsAsync();
        _grants = permissions.Match(
            p => new OrganizationGrants(p.Permissions.ToHashSet(StringComparer.Ordinal), p.IsGlobalAdmin),
            _ => OrganizationGrants.None
        );
    }

    private void NotifyStateChanged()
    {
        bus.Publish<MainLayout.AuthChanged>();
    }
}
