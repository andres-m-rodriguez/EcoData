using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using FaunaFinder.Client.Services.Account;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

// Exercises FaunaFinder's /account proxy end to end: signup against the
// faunafinder resource (its own origin and cookie), and approval through the
// shared admin session on ecoportal.
[Collection(EcoDataTestCollection.Name)]
public sealed class FaunaFinderAccountTests(EcoDataTestFixture fixture) : IDisposable
{
    private const string Password = "SecurePassword123!";

    private readonly List<IDisposable> _disposables = [];

    private IOrganizationAccessRequestHttpClient AdminAccessRequests =>
        fixture.Services.GetRequiredService<IOrganizationAccessRequestHttpClient>();

    private IOrganizationMemberHttpClient AdminMembers =>
        fixture.Services.GetRequiredService<IOrganizationMemberHttpClient>();

    private IOrganizationRoleHttpClient AdminRoles =>
        fixture.Services.GetRequiredService<IOrganizationRoleHttpClient>();

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        var client = await CreateFaunaFinderClientAsync();

        var response = await client.GetAsync("/account/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ReturnsConflict()
    {
        var client = await CreateFaunaFinderClientAsync();

        // The seeded admin always exists, and the duplicate is rejected at the
        // register step, so no login attempt is spent against the limiter.
        var response = await client.PostAsJsonAsync(
            "/account/signup",
            new FaunaFinderSignupRequest("admin@gmail.com", "Impostor", Password, Password, false)
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_NotStudent_CreatesNoAccessRequest()
    {
        var client = await CreateFaunaFinderClientAsync();

        var signup = await SignupAsync(client, isStudent: false, waitForAccessRequest: false);

        signup.AccessRequestSubmitted.Should().BeFalse();

        var requests = await client.GetFromJsonAsync<List<OrganizationAccessRequestDto>>(
            "/account/access-requests"
        );
        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Signup_AsStudent_RequestApprovalGrantsTheStudentRole()
    {
        var client = await CreateFaunaFinderClientAsync();

        var signup = await SignupAsync(client, isStudent: true, waitForAccessRequest: true);
        signup.AccessRequestSubmitted.Should().BeTrue();

        var me = await client.GetFromJsonAsync<UserInfo>("/account/me");
        me!.Email.Should().Be(signup.User.Email);

        var requests = await client.GetFromJsonAsync<List<OrganizationAccessRequestDto>>(
            "/account/access-requests"
        );
        requests.Should().ContainSingle();
        var request = requests![0];
        request.Status.Should().Be("Pending");
        request.RoleName.Should().Be("Student");

        var roles = await AdminRoles.GetListAsync(request.OrganizationId);
        roles.IsT0.Should().BeTrue("Listing the organization roles should succeed");
        roles
            .AsT0.Select(r => r.Name)
            .Should()
            .Contain(["Student", "FaunaAdministrator"], "the seeder creates both FaunaFinder roles");

        var approved = await AdminAccessRequests.UpdateStatusAsync(
            request.OrganizationId,
            request.Id,
            new UpdateOrganizationAccessRequestStatusRequest(Approved: true)
        );
        approved.IsT0.Should().BeTrue("Approval by the admin should succeed");
        approved.AsT0.Status.Should().Be("Approved");

        var member = await AdminMembers.GetAsync(request.OrganizationId, request.UserId);
        member.IsT0.Should().BeTrue("The approved requester should now be a member");
        member.AsT0.RoleName.Should().Be("Student");

        var refreshed = await client.GetFromJsonAsync<List<OrganizationAccessRequestDto>>(
            "/account/access-requests"
        );
        refreshed![0].Status.Should().Be("Approved");

        var logout = await client.PostAsync("/account/logout", null);
        logout.EnsureSuccessStatusCode();

        var afterLogout = await client.GetAsync("/account/me");
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<FaunaFinderSignupResponse> SignupAsync(
        HttpClient client,
        bool isStudent,
        bool waitForAccessRequest
    )
    {
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var email = $"faunafinder-{Guid.CreateVersion7():N}@example.com";
            var response = await client.PostAsJsonAsync(
                "/account/signup",
                new FaunaFinderSignupRequest(email, "FaunaFinder Test User", Password, Password, isStudent)
            );

            // EcoPortal's login limiter allows 3 attempts per 2 minutes per
            // caller, and in tests every login funnels through localhost.
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(TimeSpan.FromSeconds(45));
                continue;
            }

            response.EnsureSuccessStatusCode();
            var signup = (
                await response.Content.ReadFromJsonAsync<FaunaFinderSignupResponse>()
            )!;

            if (!waitForAccessRequest || signup.AccessRequestSubmitted)
            {
                return signup;
            }

            // The organization loader retries every 30 seconds until EcoPortal
            // answers; a signup landing before then creates no access request.
            await Task.Delay(TimeSpan.FromSeconds(35));
        }

        throw new InvalidOperationException(
            "Signup did not produce the expected result before the retry budget ran out."
        );
    }

    private async Task<HttpClient> CreateFaunaFinderClientAsync()
    {
        var notifications = fixture.App.Services.GetRequiredService<ResourceNotificationService>();
        await notifications
            .WaitForResourceAsync("faunafinder", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));

        using var tempClient = fixture.App.CreateHttpClient("faunafinder", "https");

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        var client = new HttpClient(handler) { BaseAddress = tempClient.BaseAddress };

        _disposables.Add(handler);
        _disposables.Add(client);
        return client;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
