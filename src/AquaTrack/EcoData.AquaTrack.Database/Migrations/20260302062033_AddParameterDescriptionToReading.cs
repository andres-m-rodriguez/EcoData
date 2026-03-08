using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterDescriptionToReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parameter_description",
                table: "readings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "parameter_description",
                table: "readings");
        }
    }
}
