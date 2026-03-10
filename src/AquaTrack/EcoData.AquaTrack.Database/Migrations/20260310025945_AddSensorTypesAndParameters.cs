using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorTypesAndParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "sensor_type_id",
                table: "sensors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sensor_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parameters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sensor_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parameters", x => x.id);
                    table.ForeignKey(
                        name: "fk_parameters_sensor_types_sensor_type_id",
                        column: x => x.sensor_type_id,
                        principalTable: "sensor_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sensors_sensor_type_id",
                table: "sensors",
                column: "sensor_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_parameters_code",
                table: "parameters",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parameters_sensor_type_id",
                table: "parameters",
                column: "sensor_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_types_code",
                table: "sensor_types",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sensors_sensor_types_sensor_type_id",
                table: "sensors",
                column: "sensor_type_id",
                principalTable: "sensor_types",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sensors_sensor_types_sensor_type_id",
                table: "sensors");

            migrationBuilder.DropTable(
                name: "parameters");

            migrationBuilder.DropTable(
                name: "sensor_types");

            migrationBuilder.DropIndex(
                name: "ix_sensors_sensor_type_id",
                table: "sensors");

            migrationBuilder.DropColumn(
                name: "sensor_type_id",
                table: "sensors");
        }
    }
}
