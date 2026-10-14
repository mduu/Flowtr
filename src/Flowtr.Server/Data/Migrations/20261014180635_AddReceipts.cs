using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowtr.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "Bookings");

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequences", x => new { x.Year, x.Kind });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReceiptId",
                table: "Bookings",
                column: "ReceiptId",
                unique: true,
                filter: "[ReceiptId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Number",
                table: "Receipts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Year_Kind_SequenceNumber",
                table: "Receipts",
                columns: new[] { "Year", "Kind", "SequenceNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Receipts_ReceiptId",
                table: "Bookings",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Receipts_ReceiptId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "ReceiptSequences");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReceiptId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
