using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoData.Organization.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessRequestRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                table: "organization_access_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_access_requests_role_id",
                table: "organization_access_requests",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_access_requests_organization_roles_role_id",
                table: "organization_access_requests",
                column: "role_id",
                principalTable: "organization_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_access_requests_organization_roles_role_id",
                table: "organization_access_requests");

            migrationBuilder.DropIndex(
                name: "ix_organization_access_requests_role_id",
                table: "organization_access_requests");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "organization_access_requests");
        }
    }
}
