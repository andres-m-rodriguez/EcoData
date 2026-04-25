using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Sensors.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPhenomenonModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_parameters_code",
                table: "parameters");

            migrationBuilder.AddColumn<double>(
                name: "canonical_value",
                table: "readings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parameter_id",
                table: "readings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "phenomenon_id",
                table: "readings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sensor_type_id",
                table: "parameters",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "phenomenon_id",
                table: "parameters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "parameters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_unit",
                table: "parameters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "unit_factor",
                table: "parameters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "unit_offset",
                table: "parameters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "value_shape",
                table: "parameters",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "phenomena",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    canonical_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    default_value_shape = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    capabilities = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phenomena", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_readings_parameter_id",
                table: "readings",
                column: "parameter_id");

            migrationBuilder.CreateIndex(
                name: "ix_readings_phenomenon_id",
                table: "readings",
                column: "phenomenon_id");

            migrationBuilder.CreateIndex(
                name: "ix_readings_sensor_id_phenomenon_id_recorded_at",
                table: "readings",
                columns: new[] { "sensor_id", "phenomenon_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_parameters_phenomenon_id",
                table: "parameters",
                column: "phenomenon_id");

            migrationBuilder.CreateIndex(
                name: "ix_parameters_source_id_code",
                table: "parameters",
                columns: new[] { "source_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_phenomena_code",
                table: "phenomena",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_parameters_phenomena_phenomenon_id",
                table: "parameters",
                column: "phenomenon_id",
                principalTable: "phenomena",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_readings_parameters_parameter_id",
                table: "readings",
                column: "parameter_id",
                principalTable: "parameters",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_readings_phenomena_phenomenon_id",
                table: "readings",
                column: "phenomenon_id",
                principalTable: "phenomena",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_parameters_phenomena_phenomenon_id",
                table: "parameters");

            migrationBuilder.DropForeignKey(
                name: "fk_readings_parameters_parameter_id",
                table: "readings");

            migrationBuilder.DropForeignKey(
                name: "fk_readings_phenomena_phenomenon_id",
                table: "readings");

            migrationBuilder.DropTable(
                name: "phenomena");

            migrationBuilder.DropIndex(
                name: "ix_readings_parameter_id",
                table: "readings");

            migrationBuilder.DropIndex(
                name: "ix_readings_phenomenon_id",
                table: "readings");

            migrationBuilder.DropIndex(
                name: "ix_readings_sensor_id_phenomenon_id_recorded_at",
                table: "readings");

            migrationBuilder.DropIndex(
                name: "ix_parameters_phenomenon_id",
                table: "parameters");

            migrationBuilder.DropIndex(
                name: "ix_parameters_source_id_code",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "canonical_value",
                table: "readings");

            migrationBuilder.DropColumn(
                name: "parameter_id",
                table: "readings");

            migrationBuilder.DropColumn(
                name: "phenomenon_id",
                table: "readings");

            migrationBuilder.DropColumn(
                name: "phenomenon_id",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "source_unit",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "unit_factor",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "unit_offset",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "value_shape",
                table: "parameters");

            migrationBuilder.AlterColumn<Guid>(
                name: "sensor_type_id",
                table: "parameters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_parameters_code",
                table: "parameters",
                column: "code",
                unique: true);
        }
    }
}
