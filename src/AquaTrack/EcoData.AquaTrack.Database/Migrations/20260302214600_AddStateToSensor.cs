using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.AquaTrack.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStateToSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "sensors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "state",
                table: "sensors");
        }
    }
}
