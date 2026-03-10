using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameSensorMunicipalityToMunicipalityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "municipality",
                table: "sensors");

            migrationBuilder.AddColumn<Guid>(
                name: "municipality_id",
                table: "sensors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensors_municipality_id",
                table: "sensors",
                column: "municipality_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sensors_municipality_id",
                table: "sensors");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                table: "sensors");

            migrationBuilder.AddColumn<string>(
                name: "municipality",
                table: "sensors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
