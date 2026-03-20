using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EcoData.Sensors.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorLocationGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "location",
                table: "sensors",
                type: "geometry(Point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensors_location",
                table: "sensors",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sensors_location",
                table: "sensors");

            migrationBuilder.DropColumn(
                name: "location",
                table: "sensors");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
