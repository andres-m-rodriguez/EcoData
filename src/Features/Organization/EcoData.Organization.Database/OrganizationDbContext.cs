using EcoData.Organization.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.Database;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : DbContext(options)
{
    public DbSet<Models.Organization> Organizations => Set<Models.Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationRole> OrganizationRoles => Set<OrganizationRole>();
    public DbSet<OrganizationRolePermission> OrganizationRolePermissions => Set<OrganizationRolePermission>();
    public DbSet<OrganizationAccessRequest> OrganizationAccessRequests => Set<OrganizationAccessRequest>();
    public DbSet<OrganizationBlockedUser> OrganizationBlockedUsers => Set<OrganizationBlockedUser>();
    public DbSet<DataSource> DataSources => Set<DataSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Models.Organization.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMember.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationRole.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationRolePermission.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationAccessRequest.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationBlockedUser.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new DataSource.EntityConfiguration());
    }
}
