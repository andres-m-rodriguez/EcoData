using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Services.Account;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

// Reporting and listing sightings on the faunafinder origin, as any signed-in
// account that is not a member of the organization, and reviewing them as the
// seeded global admin on the ecoportal origin, where the same routes are mapped.
[Collection(EcoDataTestCollection.Name)]
public sealed class FaunaFinderSightingTests(SightingReporters reporters)
    : IClassFixture<SightingReporters>
{
    [Fact]
    public async Task Report_WithoutSession_ReturnsUnauthorized()
    {
        var response = await reporters.Anonymous.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport()
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_WithoutSession_ReturnsUnauthorized()
    {
        var response = await reporters.Anonymous.GetAsync("/wildlife/me/sightings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Report_Valid_ReturnsCreatedAndAppearsFirstInMyList()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport()
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;
        sighting.Status.Should().Be(SightingStatus.Pending);
        sighting.ReporterDisplayName.Should().Be(SightingReporters.ReporterDisplayName);
        sighting.OrganizationId.Should().Be(reporters.OrganizationId);
        sighting.SpeciesId.Should().Be(reporters.SpeciesId);
        sighting.SpeciesScientificName.Should().NotBeEmpty();
        sighting.ReviewedByDisplayName.Should().BeNull();
        sighting.Notes.Should().BeEmpty();
        sighting.Images.Should().BeEmpty();
        response.Headers.Location.Should().NotBeNull();
        response
            .Headers.Location!.ToString()
            .Should()
            .EndWith($"/wildlife/organizations/{reporters.OrganizationId}/sightings/{sighting.Id}");

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );

        mine.Should().NotBeEmpty();
        mine![0].Id.Should().Be(sighting.Id);
    }

    [Fact]
    public async Task Report_UnknownSpecies_ReturnsNotFound()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport() with { SpeciesId = Guid.CreateVersion7() }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Report_LatitudeOutOfRange_ReturnsValidationProblemOnLatitude()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport() with { Latitude = 200 }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ProblemDetailsParser.ParseAsync(response, CancellationToken.None);
        problem!
            .Errors!.Keys.Should()
            .ContainSingle(key => key.Equals("latitude", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMine_ExcludesAnotherUsersReports()
    {
        var response = await reporters.Other.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport()
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var theirs = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );

        mine.Should().NotContain(sighting => sighting.Id == theirs.Id);
    }

    [Fact]
    public async Task Notes_ReporterAppends_OtherUserForbidden_BlankRejected()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport() with { Note = "Seen near the river bank" }
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        sighting.Notes.Should().ContainSingle();
        sighting.Notes[0].Text.Should().Be("Seen near the river bank");
        sighting.Notes[0].AuthorUserId.Should().Be(sighting.ReporterUserId);
        sighting.Notes[0].AuthorDisplayName.Should().Be(SightingReporters.ReporterDisplayName);

        var appended = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/sightings/{sighting.Id}/notes",
            new SightingNoteDtoForCreate("Two adults and one juvenile")
        );
        appended.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = (await appended.Content.ReadFromJsonAsync<SightingNoteDto>())!;
        note.Text.Should().Be("Two adults and one juvenile");
        note.AuthorDisplayName.Should().Be(SightingReporters.ReporterDisplayName);

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );
        var refreshed = mine!.Single(item => item.Id == sighting.Id);
        refreshed.Notes.Select(item => item.Id).Should().Equal(sighting.Notes[0].Id, note.Id);

        var forbidden = await reporters.Other.PostAsJsonAsync(
            $"/wildlife/sightings/{sighting.Id}/notes",
            new SightingNoteDtoForCreate("Not my sighting")
        );
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var blank = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/sightings/{sighting.Id}/notes",
            new SightingNoteDtoForCreate("   ")
        );
        blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ProblemDetailsParser.ParseAsync(blank, CancellationToken.None);
        problem!
            .Errors!.Keys.Should()
            .ContainSingle(key => key.Equals("text", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddNote_UnknownSighting_ReturnsNotFound()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/sightings/{Guid.CreateVersion7()}/notes",
            new SightingNoteDtoForCreate("Nobody home")
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Review_WithoutVerifyPermission_ReturnsForbidden()
    {
        var prefix = $"/wildlife/organizations/{reporters.OrganizationId}/sightings";
        var id = Guid.CreateVersion7();

        var list = await reporters.Reporter.GetAsync(prefix);
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var count = await reporters.Reporter.GetAsync($"{prefix}/count");
        count.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var approve = await reporters.Reporter.PostAsJsonAsync(
            $"{prefix}/{id}/approve",
            new SightingApprovalDto(null)
        );
        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var deny = await reporters.Reporter.PostAsJsonAsync(
            $"{prefix}/{id}/deny",
            new SightingDenialDto("Not enough detail")
        );
        deny.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var unapprove = await reporters.Reporter.PostAsync($"{prefix}/{id}/unapprove", null);
        unapprove.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_ThenUnapprove_AdminReviewsAndReporterSeesTheStatus()
    {
        var prefix = $"/wildlife/organizations/{reporters.OrganizationId}/sightings";
        var pendingBefore = await reporters.Admin.GetFromJsonAsync<int>(
            $"{prefix}/count?status=Pending"
        );

        var response = await reporters.Reporter.PostAsJsonAsync(prefix, ValidReport());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        var pending = await reporters.Admin.GetFromJsonAsync<List<SightingDto>>(
            $"{prefix}?status=Pending"
        );
        pending.Should().Contain(item => item.Id == sighting.Id);
        (await reporters.Admin.GetFromJsonAsync<int>($"{prefix}/count?status=Pending"))
            .Should()
            .Be(pendingBefore + 1);

        var approved = await reporters.Admin.PostAsJsonAsync(
            $"{prefix}/{sighting.Id}/approve",
            new SightingApprovalDto("Matches the photo on file")
        );
        approved.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reviewed = await reporters.Admin.GetFromJsonAsync<SightingDto>(
            $"{prefix}/{sighting.Id}"
        );
        reviewed!.Status.Should().Be(SightingStatus.Approved);
        reviewed.ReviewedByDisplayName.Should().NotBeNullOrEmpty();
        reviewed.ReviewedAtUtc.Should().NotBeNull();
        reviewed.ReviewReason.Should().Be("Matches the photo on file");
        (await reporters.Admin.GetFromJsonAsync<int>($"{prefix}/count?status=Pending"))
            .Should()
            .Be(pendingBefore);

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );
        mine!.Single(item => item.Id == sighting.Id).Status.Should().Be(SightingStatus.Approved);

        var unapproved = await reporters.Admin.PostAsync($"{prefix}/{sighting.Id}/unapprove", null);
        unapproved.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reset = await reporters.Admin.GetFromJsonAsync<SightingDto>($"{prefix}/{sighting.Id}");
        reset!.Status.Should().Be(SightingStatus.Pending);
        reset.ReviewedByDisplayName.Should().BeNull();
        reset.ReviewedAtUtc.Should().BeNull();
        reset.ReviewReason.Should().BeNull();
        (await reporters.Admin.GetFromJsonAsync<int>($"{prefix}/count?status=Pending"))
            .Should()
            .Be(pendingBefore + 1);
    }

    [Fact]
    public async Task Deny_WithReason_ThenUnapprove_BlankReasonRejected()
    {
        var prefix = $"/wildlife/organizations/{reporters.OrganizationId}/sightings";

        var response = await reporters.Reporter.PostAsJsonAsync(prefix, ValidReport());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        var blank = await reporters.Admin.PostAsJsonAsync(
            $"{prefix}/{sighting.Id}/deny",
            new SightingDenialDto("   ")
        );
        blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ProblemDetailsParser.ParseAsync(blank, CancellationToken.None);
        problem!
            .Errors!.Keys.Should()
            .ContainSingle(key => key.Equals("reason", StringComparison.OrdinalIgnoreCase));

        var denied = await reporters.Admin.PostAsJsonAsync(
            $"{prefix}/{sighting.Id}/deny",
            new SightingDenialDto("The point is in the middle of the ocean")
        );
        denied.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reviewed = await reporters.Admin.GetFromJsonAsync<SightingDto>(
            $"{prefix}/{sighting.Id}"
        );
        reviewed!.Status.Should().Be(SightingStatus.Denied);
        reviewed.ReviewedByDisplayName.Should().NotBeNullOrEmpty();
        reviewed.ReviewReason.Should().Be("The point is in the middle of the ocean");

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );
        var theirs = mine!.Single(item => item.Id == sighting.Id);
        theirs.Status.Should().Be(SightingStatus.Denied);
        theirs.ReviewReason.Should().Be("The point is in the middle of the ocean");

        var unapproved = await reporters.Admin.PostAsync($"{prefix}/{sighting.Id}/unapprove", null);
        unapproved.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reset = await reporters.Admin.GetFromJsonAsync<SightingDto>($"{prefix}/{sighting.Id}");
        reset!.Status.Should().Be(SightingStatus.Pending);
        reset.ReviewReason.Should().BeNull();
        reset.ReviewedByDisplayName.Should().BeNull();
    }

    [Fact]
    public async Task Notes_AdminAppends_ReporterSeesItInTheThread()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport()
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        var appended = await reporters.Admin.PostAsJsonAsync(
            $"/wildlife/sightings/{sighting.Id}/notes",
            new SightingNoteDtoForCreate("Could you add a photo?")
        );
        appended.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = (await appended.Content.ReadFromJsonAsync<SightingNoteDto>())!;
        note.AuthorUserId.Should().NotBe(sighting.ReporterUserId);

        var mine = await reporters.Reporter.GetFromJsonAsync<List<SightingDto>>(
            "/wildlife/me/sightings"
        );
        var refreshed = mine!.Single(item => item.Id == sighting.Id);
        refreshed.Notes.Should().ContainSingle(item => item.Id == note.Id);
        refreshed.Notes[0].Text.Should().Be("Could you add a photo?");
    }

    [Fact]
    public async Task Approve_UnknownSighting_ReturnsNotFound()
    {
        var response = await reporters.Admin.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings/{Guid.CreateVersion7()}/approve",
            new SightingApprovalDto(null)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_FromAnotherOrganization_ReturnsNotFound()
    {
        var response = await reporters.Reporter.PostAsJsonAsync(
            $"/wildlife/organizations/{reporters.OrganizationId}/sightings",
            ValidReport()
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sighting = (await response.Content.ReadFromJsonAsync<SightingDto>())!;

        var elsewhere = await reporters.Admin.GetAsync(
            $"/wildlife/organizations/{Guid.CreateVersion7()}/sightings/{sighting.Id}"
        );

        elsewhere.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private SightingDtoForCreate ValidReport() =>
        new(
            reporters.SpeciesId,
            Latitude: 18.4,
            Longitude: -66.1,
            MunicipalityId: null,
            ObservedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            IndividualCount: 2,
            Note: null
        );
}

// Two signed-up accounts shared by the whole class: EcoPortal's login limiter
// allows 3 attempts per 2 minutes per caller, so every test signing up its own
// user would spend most of its time waiting on 429s.
public sealed class SightingReporters(EcoDataTestFixture fixture) : IAsyncLifetime
{
    public const string ReporterDisplayName = "Sighting Reporter";
    public const string OtherDisplayName = "Another Reporter";

    private const string Password = "SecurePassword123!";

    private readonly List<IDisposable> _disposables = [];

    public HttpClient Anonymous { get; private set; } = null!;
    public HttpClient Reporter { get; private set; } = null!;
    public HttpClient Other { get; private set; } = null!;

    // The fixture's admin session on the ecoportal origin; global admins pass
    // every organization permission check without a membership.
    public HttpClient Admin { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid SpeciesId { get; private set; }

    public async Task InitializeAsync()
    {
        Anonymous = await CreateFaunaFinderClientAsync();
        Reporter = await CreateFaunaFinderClientAsync();
        Other = await CreateFaunaFinderClientAsync();
        Admin = fixture.Services.GetRequiredService<HttpClient>();

        await SignupAsync(Reporter, ReporterDisplayName);
        await SignupAsync(Other, OtherDisplayName);

        OrganizationId = (await WaitForOrganizationAsync(Anonymous)).Id;

        // The list answers PageSize + 1 rows as its next-page probe.
        var species = await Anonymous.GetFromJsonAsync<List<SpeciesDtoForList>>(
            "/wildlife/species?pageSize=1"
        );
        SpeciesId = species![0].Id;
    }

    public Task DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }

    private static async Task SignupAsync(HttpClient client, string displayName)
    {
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var email = $"sighting-{Guid.CreateVersion7():N}@example.com";
            var response = await client.PostAsJsonAsync(
                "/account/signup",
                new FaunaFinderSignupRequest(email, displayName, Password, Password, false)
            );

            // EcoPortal's login limiter allows 3 attempts per 2 minutes per
            // caller, and in tests every login funnels through localhost.
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(TimeSpan.FromSeconds(45));
                continue;
            }

            response.EnsureSuccessStatusCode();
            return;
        }

        throw new InvalidOperationException(
            "Signup did not succeed before the retry budget ran out."
        );
    }

    // The organization loader retries every 30 seconds until EcoPortal
    // answers, and the endpoint reports 503 until then.
    private static async Task<FaunaFinderOrganizationDto> WaitForOrganizationAsync(
        HttpClient client
    )
    {
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var response = await client.GetAsync("/account/organization");

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                await Task.Delay(TimeSpan.FromSeconds(35));
                continue;
            }

            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<FaunaFinderOrganizationDto>())!;
        }

        throw new InvalidOperationException(
            "The FaunaFinder organization was not resolved before the retry budget ran out."
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
        var client = new HttpClient(new AccountLimiterRetryHandler(handler))
        {
            BaseAddress = tempClient.BaseAddress,
        };

        _disposables.Add(handler);
        _disposables.Add(client);
        return client;
    }

    // FaunaFinder's own /account bucket allows 12 requests per minute per
    // client. Its 429 carries Retry-After (at most the 10 second replenishment
    // period); EcoPortal's login 429 is relayed without one and stays with
    // SignupAsync.
    private sealed class AccountLimiterRetryHandler(HttpMessageHandler inner)
        : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            for (var attempt = 1; ; attempt++)
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (
                    attempt == 4
                    || response.StatusCode != HttpStatusCode.TooManyRequests
                    || response.Headers.RetryAfter?.Delta is not { } retryAfter
                )
                {
                    return response;
                }

                response.Dispose();
                await Task.Delay(retryAfter + TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
