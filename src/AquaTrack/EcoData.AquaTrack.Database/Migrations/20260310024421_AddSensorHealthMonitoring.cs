using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorHealthMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sensors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "sensor_health_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_health_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_health_alerts_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensor_health_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    stale_threshold_seconds = table.Column<int>(type: "integer", nullable: false),
                    unhealthy_threshold_seconds = table.Column<int>(type: "integer", nullable: false),
                    is_monitoring_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_health_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_health_configs_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensor_health_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_reading_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    last_error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_health_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_health_statuses_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_alerts_sensor_id",
                table: "sensor_health_alerts",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_alerts_sensor_id_resolved_at",
                table: "sensor_health_alerts",
                columns: new[] { "sensor_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_alerts_triggered_at",
                table: "sensor_health_alerts",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_configs_sensor_id",
                table: "sensor_health_configs",
                column: "sensor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_statuses_sensor_id",
                table: "sensor_health_statuses",
                column: "sensor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensor_health_statuses_status",
                table: "sensor_health_statuses",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensor_health_alerts");

            migrationBuilder.DropTable(
                name: "sensor_health_configs");

            migrationBuilder.DropTable(
                name: "sensor_health_statuses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sensors");
        }
    }
}
