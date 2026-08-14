using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveSpeciesProfileImagesToBlobStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Profile images move to blob storage; the row keeps only the name of
            // the blob holding them, which is also what every "has an image"
            // filter and DTO flag now reads.
            //
            // The bytes cannot be copied across in SQL, so this drops them: the
            // seeder re-uploads from Data/species.json on its next run for every
            // species whose profile_image_blob_name is null. Images that only
            // ever existed in this column would not come back — there are none:
            // the seeder is the only thing that has ever written it.
            migrationBuilder.DropColumn(
                name: "profile_image_data",
                table: "species");

            migrationBuilder.AddColumn<string>(
                name: "profile_image_blob_name",
                table: "species",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_image_blob_name",
                table: "species");

            migrationBuilder.AddColumn<byte[]>(
                name: "profile_image_data",
                table: "species",
                type: "bytea",
                nullable: true);
        }
    }
}
