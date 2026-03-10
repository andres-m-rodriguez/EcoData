using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedSensorTypesAndParameters : Migration
    {
        // Well-known GUIDs for sensor types
        private static readonly Guid WaterQualityTypeId = Guid.Parse("01943e00-0000-7000-8000-000000000001");
        private static readonly Guid FlowTypeId = Guid.Parse("01943e00-0000-7000-8000-000000000002");
        private static readonly Guid LevelTypeId = Guid.Parse("01943e00-0000-7000-8000-000000000003");
        private static readonly Guid TemperatureTypeId = Guid.Parse("01943e00-0000-7000-8000-000000000004");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTimeOffset.UtcNow;

            // Seed Sensor Types
            migrationBuilder.InsertData(
                table: "sensor_types",
                columns: ["id", "code", "name", "description", "created_at"],
                values: new object[,]
                {
                    { WaterQualityTypeId, "WATER_QUALITY", "Water Quality", "Sensors measuring water quality parameters like pH, dissolved oxygen, etc.", now },
                    { FlowTypeId, "FLOW", "Flow", "Sensors measuring water flow and discharge", now },
                    { LevelTypeId, "LEVEL", "Level", "Sensors measuring water level and gage height", now },
                    { TemperatureTypeId, "TEMPERATURE", "Temperature", "Sensors measuring water temperature", now }
                });

            // Seed Parameters - mapped to USGS parameter codes
            migrationBuilder.InsertData(
                table: "parameters",
                columns: ["id", "code", "name", "default_unit", "sensor_type_id", "created_at"],
                values: new object[,]
                {
                    // Flow parameters
                    { Guid.Parse("01943e00-0001-7000-8000-000000000001"), "00060", "Discharge", "ft\u00b3/s", FlowTypeId, now },

                    // Level parameters
                    { Guid.Parse("01943e00-0001-7000-8000-000000000002"), "00065", "Gage Height", "ft", LevelTypeId, now },

                    // Temperature parameters
                    { Guid.Parse("01943e00-0001-7000-8000-000000000003"), "00010", "Temperature, water", "\u00b0C", TemperatureTypeId, now },

                    // Water Quality parameters
                    { Guid.Parse("01943e00-0001-7000-8000-000000000004"), "00400", "pH", "std units", WaterQualityTypeId, now },
                    { Guid.Parse("01943e00-0001-7000-8000-000000000005"), "00300", "Dissolved Oxygen", "mg/L", WaterQualityTypeId, now },
                    { Guid.Parse("01943e00-0001-7000-8000-000000000006"), "00095", "Specific Conductance", "\u03bcS/cm", WaterQualityTypeId, now },
                    { Guid.Parse("01943e00-0001-7000-8000-000000000007"), "00076", "Turbidity", "NTU", WaterQualityTypeId, now }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete parameters first (foreign key constraint)
            migrationBuilder.DeleteData(
                table: "parameters",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("01943e00-0001-7000-8000-000000000001"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000002"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000003"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000004"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000005"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000006"),
                    Guid.Parse("01943e00-0001-7000-8000-000000000007")
                });

            // Delete sensor types
            migrationBuilder.DeleteData(
                table: "sensor_types",
                keyColumn: "id",
                keyValues: new object[]
                {
                    WaterQualityTypeId,
                    FlowTypeId,
                    LevelTypeId,
                    TemperatureTypeId
                });
        }
    }
}
