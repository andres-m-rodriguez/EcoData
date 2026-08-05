using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Sensors.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSurfaceWaterSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reading_stats_snapshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    total_readings = table.Column<long>(type: "bigint", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reading_stats_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "surface_water_station_snapshots",
                columns: table => new
                {
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    municipality_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    latest_streamflow_cfs = table.Column<double>(type: "double precision", nullable: true),
                    latest_gage_height_ft = table.Column<double>(type: "double precision", nullable: true),
                    latest_recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sparkline_flow = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surface_water_station_snapshots", x => x.sensor_id);
                });

            migrationBuilder.CreateTable(
                name: "surface_water_summary_snapshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    total_stations = table.Column<int>(type: "integer", nullable: false),
                    stations_reporting = table.Column<int>(type: "integer", nullable: false),
                    readings7d = table.Column<long>(type: "bigint", nullable: false),
                    median_streamflow_cfs = table.Column<double>(type: "double precision", nullable: true),
                    mean_gage_height_ft = table.Column<double>(type: "double precision", nullable: true),
                    mean_rainfall_inches7d = table.Column<double>(type: "double precision", nullable: true),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surface_water_summary_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_surface_water_station_snapshots_rank",
                table: "surface_water_station_snapshots",
                column: "rank",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reading_stats_snapshots");

            migrationBuilder.DropTable(
                name: "surface_water_station_snapshots");

            migrationBuilder.DropTable(
                name: "surface_water_summary_snapshots");
        }
    }
}
