using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Dtos;
using EcoData.Identity.Database;
using EcoData.IntegrationTests.Bases;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Services;

public sealed class UserLookupServiceTests(EcoDataTestFixture fixture) : ServiceTestBase(fixture)
{
    private IUserLookupService UserLookup =>
        DomainServices.GetRequiredService<IUserLookupService>();

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // The seeder creates an admin user - find them first
        var users = await UserLookup.GetByIdsAsync([]);

        // Get a known user by querying with email pattern
        // Note: In a real scenario, you'd have a test store for users like OrganizationsTestStore
        var adminUser = await GetAdminUserAsync();
        adminUser.Should().NotBeNull("Admin user should exist from seeder");

        var result = await UserLookup.GetByIdAsync(adminUser!.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(adminUser.Id);
        result.Email.Should().Be(adminUser.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var nonExistentId = Guid.CreateVersion7();

        var result = await UserLookup.GetByIdAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenUserExists_ReturnsTrue()
    {
        var adminUser = await GetAdminUserAsync();
        adminUser.Should().NotBeNull();

        var exists = await UserLookup.ExistsAsync(adminUser!.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var nonExistentId = Guid.CreateVersion7();

        var exists = await UserLookup.ExistsAsync(nonExistentId);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task IsGlobalAdminAsync_WhenUserIsGlobalAdmin_ReturnsTrue()
    {
        var adminUser = await GetAdminUserAsync();
        adminUser.Should().NotBeNull();

        var isAdmin = await UserLookup.IsGlobalAdminAsync(adminUser!.Id);

        isAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task IsGlobalAdminAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var nonExistentId = Guid.CreateVersion7();

        var isAdmin = await UserLookup.IsGlobalAdminAsync(nonExistentId);

        isAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdsAsync_WhenUsersExist_ReturnsDictionary()
    {
        var adminUser = await GetAdminUserAsync();
        adminUser.Should().NotBeNull();

        var results = await UserLookup.GetByIdsAsync([adminUser!.Id]);

        results.Should().ContainKey(adminUser.Id);
        results[adminUser.Id].Email.Should().Be(adminUser.Email);
    }

    [Fact]
    public async Task GetByIdsAsync_WhenEmpty_ReturnsEmptyDictionary()
    {
        var results = await UserLookup.GetByIdsAsync([]);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_WhenMixedExistence_ReturnsOnlyExisting()
    {
        var adminUser = await GetAdminUserAsync();
        adminUser.Should().NotBeNull();

        var nonExistentId = Guid.CreateVersion7();
        var results = await UserLookup.GetByIdsAsync([adminUser!.Id, nonExistentId]);

        results.Should().ContainKey(adminUser.Id);
        results.Should().NotContainKey(nonExistentId);
        results.Should().HaveCount(1);
    }

    private async Task<UserLookupDto?> GetAdminUserAsync()
    {
        // Query using the database context to find the admin user
        // This is a bootstrap method - in production tests you'd have a UsersTestStore
        using var scope = DomainServices.CreateScope();
        var context = scope
            .ServiceProvider.GetRequiredService<
                IDbContextFactory<EcoData.Identity.Database.IdentityDbContext>
            >()
            .CreateDbContext();

        var admin = await context
            .Users.Where(u => u.Email == "admin@gmail.com")
            .Select(u => new UserLookupDto(u.Id, u.Email!, u.DisplayName))
            .FirstOrDefaultAsync();

        return admin;
    }
}
