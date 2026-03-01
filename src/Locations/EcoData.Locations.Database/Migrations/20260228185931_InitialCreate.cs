using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EcoData.Locations.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    fips_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    boundary = table.Column<Geometry>(type: "geometry(Geometry, 4326)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "municipalities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    geo_json_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    county_fips_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    boundary = table.Column<Geometry>(type: "geometry(Geometry, 4326)", nullable: true),
                    centroid_latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    centroid_longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipalities", x => x.id);
                    table.ForeignKey(
                        name: "fk_municipalities_states_state_id",
                        column: x => x.state_id,
                        principalTable: "states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_municipalities_boundary",
                table: "municipalities",
                column: "boundary")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "ix_municipalities_geo_json_id",
                table: "municipalities",
                column: "geo_json_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_municipalities_state_id",
                table: "municipalities",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_states_code",
                table: "states",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_states_fips_code",
                table: "states",
                column: "fips_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "states");
        }
    }
}
