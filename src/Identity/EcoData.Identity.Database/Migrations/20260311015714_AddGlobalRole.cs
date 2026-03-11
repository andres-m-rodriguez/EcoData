using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Identity.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "global_role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "global_role",
                table: "users");
        }
    }
}
