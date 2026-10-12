using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Flowtr.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvitedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3f1b5d2e-8a4c-4e6f-9b7d-1c2a3e4f5a60", "3f1b5d2e-8a4c-4e6f-9b7d-1c2a3e4f5a60", "Admin", "ADMIN" },
                    { "7c9e2a4b-1d3f-4b5a-8e6c-2f4a6b8d0c11", "7c9e2a4b-1d3f-4b5a-8e6c-2f4a6b8d0c11", "Contributor", "CONTRIBUTOR" }
                });

            // Existing users registered themselves before invitations: they keep their access as contributors.
            migrationBuilder.Sql("UPDATE AspNetUsers SET InvitedAt = SYSDATETIMEOFFSET()");
            migrationBuilder.Sql(
                "INSERT INTO AspNetUserRoles (UserId, RoleId) SELECT Id, '7c9e2a4b-1d3f-4b5a-8e6c-2f4a6b8d0c11' FROM AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3f1b5d2e-8a4c-4e6f-9b7d-1c2a3e4f5a60");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7c9e2a4b-1d3f-4b5a-8e6c-2f4a6b8d0c11");

            migrationBuilder.DropColumn(
                name: "InvitedAt",
                table: "AspNetUsers");
        }
    }
}
