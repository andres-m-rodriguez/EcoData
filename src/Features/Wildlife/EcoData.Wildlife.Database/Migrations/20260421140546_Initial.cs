using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "fws_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fws_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nrcs_practices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nrcs_practices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "species",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scientific_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_image_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    profile_image_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    image_source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_fauna = table.Column<bool>(type: "boolean", nullable: false),
                    el_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    g_rank = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    s_rank = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    common_name = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "species_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fws_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nrcs_practice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fws_action_id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    justification = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fws_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_fws_links_fws_actions_fws_action_id",
                        column: x => x.fws_action_id,
                        principalTable: "fws_actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fws_links_nrcs_practices_nrcs_practice_id",
                        column: x => x.nrcs_practice_id,
                        principalTable: "nrcs_practices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fws_links_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "municipality_species",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    municipality_id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipality_species", x => x.id);
                    table.ForeignKey(
                        name: "fk_municipality_species_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "species_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    radius_meters = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_species_locations_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_species",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    common_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scientific_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    photo_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    photo_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_endangered = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_species_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_species", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_species_species_approved_species_id",
                        column: x => x.approved_species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "species_category_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species_category_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_species_category_links_species_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "species_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_species_category_links_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sightings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_species_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    count = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    behaviors = table.Column<int>(type: "integer", nullable: false),
                    evidence = table.Column<int>(type: "integer", nullable: false),
                    weather = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    municipality_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    photo_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    photo_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    audio_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    audio_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_flagged_for_review = table.Column<bool>(type: "boolean", nullable: false),
                    is_new_municipality_record = table.Column<bool>(type: "boolean", nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sightings", x => x.id);
                    table.CheckConstraint("CK_sightings_species_reference", "species_id IS NOT NULL OR user_species_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_sightings_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sightings_user_species_user_species_id",
                        column: x => x.user_species_id,
                        principalTable: "user_species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "fws_actions_code_uidx",
                table: "fws_actions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fws_links_practice_action_species_idx",
                table: "fws_links",
                columns: new[] { "nrcs_practice_id", "fws_action_id", "species_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fws_links_fws_action_id",
                table: "fws_links",
                column: "fws_action_id");

            migrationBuilder.CreateIndex(
                name: "ix_fws_links_species_id",
                table: "fws_links",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "ix_municipality_species_species_id",
                table: "municipality_species",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "municipality_species_municipality_id_idx",
                table: "municipality_species",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "municipality_species_municipality_species_uidx",
                table: "municipality_species",
                columns: new[] { "municipality_id", "species_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "nrcs_practices_code_uidx",
                table: "nrcs_practices",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sightings_species_id",
                table: "sightings",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "ix_sightings_user_species_id",
                table: "sightings",
                column: "user_species_id");

            migrationBuilder.CreateIndex(
                name: "sightings_is_flagged_for_review_idx",
                table: "sightings",
                column: "is_flagged_for_review");

            migrationBuilder.CreateIndex(
                name: "sightings_location_gist_idx",
                table: "sightings",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "sightings_municipality_id_idx",
                table: "sightings",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "sightings_observed_at_idx",
                table: "sightings",
                column: "observed_at");

            migrationBuilder.CreateIndex(
                name: "sightings_reported_by_user_id_idx",
                table: "sightings",
                column: "reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "sightings_status_idx",
                table: "sightings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "species_scientific_name_uidx",
                table: "species",
                column: "scientific_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "species_categories_code_uidx",
                table: "species_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_species_category_links_category_id",
                table: "species_category_links",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "species_category_links_species_category_uidx",
                table: "species_category_links",
                columns: new[] { "species_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "species_locations_species_id_idx",
                table: "species_locations",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_species_approved_species_id",
                table: "user_species",
                column: "approved_species_id");

            migrationBuilder.CreateIndex(
                name: "user_species_common_name_idx",
                table: "user_species",
                column: "common_name");

            migrationBuilder.CreateIndex(
                name: "user_species_created_by_user_id_idx",
                table: "user_species",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "user_species_is_verified_idx",
                table: "user_species",
                column: "is_verified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fws_links");

            migrationBuilder.DropTable(
                name: "municipality_species");

            migrationBuilder.DropTable(
                name: "sightings");

            migrationBuilder.DropTable(
                name: "species_category_links");

            migrationBuilder.DropTable(
                name: "species_locations");

            migrationBuilder.DropTable(
                name: "fws_actions");

            migrationBuilder.DropTable(
                name: "nrcs_practices");

            migrationBuilder.DropTable(
                name: "user_species");

            migrationBuilder.DropTable(
                name: "species_categories");

            migrationBuilder.DropTable(
                name: "species");
        }
    }
}
