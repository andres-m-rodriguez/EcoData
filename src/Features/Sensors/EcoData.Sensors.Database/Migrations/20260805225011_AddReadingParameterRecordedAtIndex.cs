using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Sensors.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingParameterRecordedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_readings_parameter_recorded_at",
                table: "readings",
                columns: new[] { "parameter", "recorded_at" })
                .Annotation("Npgsql:IndexInclude", new[] { "sensor_id", "value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_readings_parameter_recorded_at",
                table: "readings");
        }
    }
}
