using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Organization.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationBrandingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accent_color",
                table: "organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "founded_year",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_status",
                table: "organizations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tagline",
                table: "organizations",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_id",
                table: "organizations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accent_color",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "founded_year",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "legal_status",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "location",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "tagline",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "tax_id",
                table: "organizations");
        }
    }
}
