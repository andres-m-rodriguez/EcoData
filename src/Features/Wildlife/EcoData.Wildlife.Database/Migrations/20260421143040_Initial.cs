using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fws_links");

            migrationBuilder.DropTable(
                name: "municipality_species");

            migrationBuilder.DropTable(
                name: "species_category_links");

            migrationBuilder.DropTable(
                name: "species_locations");

            migrationBuilder.DropTable(
                name: "fws_actions");

            migrationBuilder.DropTable(
                name: "nrcs_practices");

            migrationBuilder.DropTable(
                name: "species_categories");

            migrationBuilder.DropTable(
                name: "species");
        }
    }
}
