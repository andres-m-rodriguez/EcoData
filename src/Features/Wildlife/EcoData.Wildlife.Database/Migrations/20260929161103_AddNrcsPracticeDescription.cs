using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Wildlife.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNrcsPracticeDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "nrcs_practices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nrcs_url",
                table: "nrcs_practices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "nrcs_practices");

            migrationBuilder.DropColumn(
                name: "nrcs_url",
                table: "nrcs_practices");
        }
    }
}
