using EcoData.IntegrationTests.Bases;
using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SensorPermissions = EcoData.Sensors.Contracts.Permissions;

namespace EcoData.IntegrationTests.Authenticated;

public sealed class OrganizationRoleTests(EcoDataTestFixture fixture) : AuthenticatedTestBase(fixture)
{
    IOrganizationHttpClient OrganizationHttpClient =>
        Services.GetRequiredService<IOrganizationHttpClient>();
    IOrganizationRoleHttpClient RoleHttpClient =>
        Services.GetRequiredService<IOrganizationRoleHttpClient>();
    IOrganizationAccessRequestHttpClient AccessRequestHttpClient =>
        Services.GetRequiredService<IOrganizationAccessRequestHttpClient>();

    [Fact]
    public async Task CreateRole_WithPermissions_AppearsInList()
    {
        var orgId = await CreateOrganizationAsync();

        var created = await RoleHttpClient.CreateAsync(
            orgId,
            new OrganizationRoleRequest(
                "Field Technician",
                [SensorPermissions.Sensor.Update, SensorPermissions.Sensor.Read]
            )
        );

        created.IsT0.Should().BeTrue("Role creation should succeed");
        created.AsT0.Name.Should().Be("Field Technician");
        created
            .AsT0.Permissions.Should()
            .Equal(SensorPermissions.Sensor.Read, SensorPermissions.Sensor.Update);
        created.AsT0.MemberCount.Should().Be(0);

        var roles = await RoleHttpClient.GetAllAsync(orgId);
        roles.IsT0.Should().BeTrue();
        roles.AsT0.Should().ContainSingle(r => r.Name == "Field Technician");
    }

    [Fact]
    public async Task UpdateRole_ReplacesNameAndPermissions()
    {
        var orgId = await CreateOrganizationAsync();
        var viewer = await RoleByNameAsync(orgId, DefaultOrganizationRoles.Viewer);

        var updated = await RoleHttpClient.UpdateAsync(
            orgId,
            viewer.Id,
            new OrganizationRoleRequest(
                "Observer",
                [SensorPermissions.Sensor.Read, Permissions.Organization.Update]
            )
        );
        updated.IsT0.Should().BeTrue("Role update should succeed");
        updated.AsT0.Name.Should().Be("Observer");
        updated
            .AsT0.Permissions.Should()
            .Equal(Permissions.Organization.Update, SensorPermissions.Sensor.Read);

        var narrowed = await RoleHttpClient.UpdateAsync(
            orgId,
            viewer.Id,
            new OrganizationRoleRequest("Observer", [SensorPermissions.Sensor.Read])
        );
        narrowed.IsT0.Should().BeTrue();
        narrowed.AsT0.Permissions.Should().Equal(SensorPermissions.Sensor.Read);
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ReturnsConflict()
    {
        var orgId = await CreateOrganizationAsync();

        var result = await RoleHttpClient.CreateAsync(
            orgId,
            new OrganizationRoleRequest(DefaultOrganizationRoles.Viewer, [])
        );

        result.IsT1.Should().BeTrue("A second role with the same name should be rejected");
        result.AsT1.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task DeleteRole_Unused_Succeeds()
    {
        var orgId = await CreateOrganizationAsync();
        var viewer = await RoleByNameAsync(orgId, DefaultOrganizationRoles.Viewer);

        var deleted = await RoleHttpClient.DeleteAsync(orgId, viewer.Id);

        deleted.IsT0.Should().BeTrue("Deleting an unused role should succeed");

        var roles = await RoleHttpClient.GetAllAsync(orgId);
        roles.AsT0.Should().NotContain(r => r.Id == viewer.Id);
    }

    [Fact]
    public async Task DeleteRole_HeldByMember_ReturnsConflict()
    {
        var orgId = await CreateOrganizationAsync();
        var contributor = await RoleByNameAsync(orgId, DefaultOrganizationRoles.Contributor);

        var request = await AccessRequestHttpClient.CreateAsync(
            orgId,
            new CreateOrganizationAccessRequestRequest(orgId, DefaultOrganizationRoles.Contributor)
        );
        request.IsT0.Should().BeTrue();
        var approved = await AccessRequestHttpClient.UpdateStatusAsync(
            orgId,
            request.AsT0.Id,
            new UpdateOrganizationAccessRequestStatusRequest(Approved: true)
        );
        approved.IsT0.Should().BeTrue();

        var deleted = await RoleHttpClient.DeleteAsync(orgId, contributor.Id);

        deleted.IsT1.Should().BeTrue("A role with members should not be deletable");
        deleted.AsT1.StatusCode.Should().Be(409);

        var roles = await RoleHttpClient.GetAllAsync(orgId);
        roles.AsT0.Should().ContainSingle(r => r.Id == contributor.Id && r.MemberCount == 1);
    }

    private async Task<OrganizationRoleDto> RoleByNameAsync(Guid orgId, string name)
    {
        var roles = await RoleHttpClient.GetAllAsync(orgId);
        roles.IsT0.Should().BeTrue("Listing roles should succeed");

        return roles.AsT0.Single(r => r.Name == name);
    }

    private async Task<Guid> CreateOrganizationAsync()
    {
        var orgName = $"Test Org Roles {Guid.CreateVersion7():N}";
        var createResult = await OrganizationHttpClient.CreateAsync(
            new OrganizationDtoForCreate(orgName, null, null, null, null)
        );
        createResult.IsT0.Should().BeTrue("Organization creation should succeed");

        return createResult.AsT0.Id;
    }
}
