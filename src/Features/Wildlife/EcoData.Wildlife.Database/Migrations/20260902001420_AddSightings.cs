using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSightings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sightings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    municipality_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    individual_count = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sightings", x => x.id);
                    table.ForeignKey(
                        name: "fk_sightings_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sighting_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sighting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blob_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sighting_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_sighting_images_sightings_sighting_id",
                        column: x => x.sighting_id,
                        principalTable: "sightings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sighting_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sighting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sighting_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_sighting_notes_sightings_sighting_id",
                        column: x => x.sighting_id,
                        principalTable: "sightings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "sighting_images_sighting_ix",
                table: "sighting_images",
                columns: new[] { "sighting_id", "id" });

            migrationBuilder.CreateIndex(
                name: "sighting_notes_sighting_ix",
                table: "sighting_notes",
                columns: new[] { "sighting_id", "id" });

            migrationBuilder.CreateIndex(
                name: "sightings_org_status_ix",
                table: "sightings",
                columns: new[] { "organization_id", "status", "id" });

            migrationBuilder.CreateIndex(
                name: "sightings_reporter_ix",
                table: "sightings",
                columns: new[] { "reporter_user_id", "id" });

            migrationBuilder.CreateIndex(
                name: "sightings_species_id_ix",
                table: "sightings",
                column: "species_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sighting_images");

            migrationBuilder.DropTable(
                name: "sighting_notes");

            migrationBuilder.DropTable(
                name: "sightings");
        }
    }
}
