using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Organization.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add the column nullable so we can backfill from the existing Name.
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "organizations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            // Step 2: derive a slug from each row's name. Lowercases, replaces any run
            // of non-alphanumeric characters with a hyphen, and trims leading/trailing
            // hyphens. Mirrors the C# SlugGenerator (without diacritic stripping —
            // pure-ASCII names are the only ones currently in any environment).
            // Collisions get a numeric suffix based on creation order.
            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT id,
                           base_slug,
                           ROW_NUMBER() OVER (PARTITION BY base_slug ORDER BY created_at) AS rn
                    FROM (
                        SELECT id,
                               created_at,
                               trim(both '-' from regexp_replace(lower(name), '[^a-z0-9]+', '-', 'g')) AS base_slug
                        FROM organizations
                    ) s
                )
                UPDATE organizations o
                SET slug = CASE WHEN n.rn = 1 THEN n.base_slug ELSE n.base_slug || '-' || n.rn END
                FROM numbered n
                WHERE o.id = n.id;
                """);

            // Step 3: now every row has a value, enforce NOT NULL and add the unique index.
            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "organizations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_slug",
                table: "organizations",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_slug",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "organizations");
        }
    }
}
