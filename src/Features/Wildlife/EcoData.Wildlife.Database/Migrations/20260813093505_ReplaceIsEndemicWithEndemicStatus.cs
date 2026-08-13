using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsEndemicWithEndemicStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "species_is_endemic_ix",
                table: "species");

            migrationBuilder.DropColumn(
                name: "is_endemic",
                table: "species");

            migrationBuilder.AddColumn<string>(
                name: "endemic_status",
                table: "species",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.CreateIndex(
                name: "species_endemic_status_ix",
                table: "species",
                column: "endemic_status");

            // Every stored iucn_status was produced by the seeder's MapGRankToIucn
            // fallback, which relabelled NatureServe G-ranks (G1 → CR, G2 → EN, …)
            // as IUCN Red List categories. They are two different assessment systems,
            // so none of these values were ever an IUCN assessment. Verified against
            // production: all 79 rows reproduced the mapping exactly, 0 divergences.
            //
            // Dropping them to NULL so the column means "no assessment on record"
            // until the importer populates real Red List data. Not reversible — the
            // old values are recomputable from g_rank but were never meaningful.
            migrationBuilder.Sql("UPDATE species SET iucn_status = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "species_endemic_status_ix",
                table: "species");

            migrationBuilder.DropColumn(
                name: "endemic_status",
                table: "species");

            migrationBuilder.AddColumn<bool>(
                name: "is_endemic",
                table: "species",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "species_is_endemic_ix",
                table: "species",
                column: "is_endemic",
                filter: "is_endemic = true");
        }
    }
}
