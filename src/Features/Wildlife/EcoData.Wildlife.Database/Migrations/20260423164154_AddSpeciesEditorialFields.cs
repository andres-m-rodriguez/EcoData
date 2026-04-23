using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeciesEditorialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "species",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "habitat",
                table: "species",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_endemic",
                table: "species",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                table: "species",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "iucn_status",
                table: "species",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_observed_at_utc",
                table: "species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "species_is_endemic_ix",
                table: "species",
                column: "is_endemic",
                filter: "is_endemic = true");

            migrationBuilder.CreateIndex(
                name: "species_is_featured_ix",
                table: "species",
                column: "is_featured",
                filter: "is_featured = true");

            migrationBuilder.CreateIndex(
                name: "species_iucn_status_ix",
                table: "species",
                column: "iucn_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "species_is_endemic_ix",
                table: "species");

            migrationBuilder.DropIndex(
                name: "species_is_featured_ix",
                table: "species");

            migrationBuilder.DropIndex(
                name: "species_iucn_status_ix",
                table: "species");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "species");

            migrationBuilder.DropColumn(
                name: "habitat",
                table: "species");

            migrationBuilder.DropColumn(
                name: "is_endemic",
                table: "species");

            migrationBuilder.DropColumn(
                name: "is_featured",
                table: "species");

            migrationBuilder.DropColumn(
                name: "iucn_status",
                table: "species");

            migrationBuilder.DropColumn(
                name: "last_observed_at_utc",
                table: "species");
        }
    }
}
