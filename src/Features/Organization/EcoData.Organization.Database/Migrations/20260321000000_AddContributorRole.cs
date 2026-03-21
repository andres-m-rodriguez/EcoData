using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Organization.Database.Migrations;

/// <inheritdoc />
public partial class AddContributorRole : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Contributor role to all existing organizations that don't have it
        migrationBuilder.Sql("""
            INSERT INTO organization.organization_roles (id, organization_id, name, created_at)
            SELECT
                gen_random_uuid(),
                o.id,
                'Contributor',
                NOW()
            FROM organization.organizations o
            WHERE NOT EXISTS (
                SELECT 1
                FROM organization.organization_roles r
                WHERE r.organization_id = o.id AND r.name = 'Contributor'
            )
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove Contributor role from all organizations
        migrationBuilder.Sql("""
            DELETE FROM organization.organization_roles
            WHERE name = 'Contributor'
            """);
    }
}
