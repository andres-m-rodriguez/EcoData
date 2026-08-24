using EcoData.IntegrationTests.Bases;
using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class OrganizationAccessRequestTests(EcoDataTestFixture fixture)
    : AuthenticatedTestBase(fixture)
{
    IOrganizationHttpClient OrganizationHttpClient =>
        Services.GetRequiredService<IOrganizationHttpClient>();
    IOrganizationAccessRequestHttpClient AccessRequestHttpClient =>
        Services.GetRequiredService<IOrganizationAccessRequestHttpClient>();
    IOrganizationRoleHttpClient RoleHttpClient =>
        Services.GetRequiredService<IOrganizationRoleHttpClient>();
    IOrganizationMemberHttpClient MemberHttpClient =>
        Services.GetRequiredService<IOrganizationMemberHttpClient>();

    [Fact]
    public async Task GetRoles_NewOrganization_ListsTheDefaultRoles()
    {
        var orgId = await CreateOrganizationAsync();

        var roles = await RoleHttpClient.GetListAsync(orgId);

        roles.IsT0.Should().BeTrue("Listing roles should succeed");
        roles
            .AsT0.Select(r => r.Name)
            .Should()
            .BeEquivalentTo(
                DefaultOrganizationRoles.Owner,
                DefaultOrganizationRoles.Admin,
                DefaultOrganizationRoles.Contributor,
                DefaultOrganizationRoles.Viewer
            );
    }

    [Fact]
    public async Task RequestAccess_WithRole_ApprovalGrantsThatRole()
    {
        var orgId = await CreateOrganizationAsync();

        var created = await AccessRequestHttpClient.CreateAsync(
            orgId,
            new CreateOrganizationAccessRequestRequest(
                orgId,
                DefaultOrganizationRoles.Contributor,
                "I would like to contribute sightings"
            )
        );
        created.IsT0.Should().BeTrue("Access request creation should succeed");
        created.AsT0.RoleName.Should().Be(DefaultOrganizationRoles.Contributor);

        var approved = await AccessRequestHttpClient.UpdateStatusAsync(
            orgId,
            created.AsT0.Id,
            new UpdateOrganizationAccessRequestStatusRequest(Approved: true)
        );
        approved.IsT0.Should().BeTrue("Approval should succeed");
        approved.AsT0.Status.Should().Be("Approved");
        approved.AsT0.RoleName.Should().Be(DefaultOrganizationRoles.Contributor);

        var member = await MemberHttpClient.GetAsync(orgId, created.AsT0.UserId);
        member.IsT0.Should().BeTrue("The approved requester should now be a member");
        member.AsT0.RoleName.Should().Be(DefaultOrganizationRoles.Contributor);
    }

    [Fact]
    public async Task RequestAccess_WithUnknownRole_ReturnsBadRequest()
    {
        var orgId = await CreateOrganizationAsync();

        var result = await AccessRequestHttpClient.CreateAsync(
            orgId,
            new CreateOrganizationAccessRequestRequest(orgId, "Wizard")
        );

        result.IsT1.Should().BeTrue("A role the organization does not have should be rejected");
        result.AsT1.StatusCode.Should().Be(400);
    }

    private async Task<Guid> CreateOrganizationAsync()
    {
        var orgName = $"Test Org Access {Guid.CreateVersion7():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        createResult.IsT0.Should().BeTrue("Organization creation should succeed");

        return createResult.AsT0.Id;
    }
}
