using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowtr.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFilesToReceiptsAndInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastFilePosition",
                table: "Receipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InboxFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboxItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    UploadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboxFiles_InboxItems_InboxItemId",
                        column: x => x.InboxItemId,
                        principalTable: "InboxItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptFiles_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxFiles_InboxItemId",
                table: "InboxFiles",
                column: "InboxItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxFiles_Sha256",
                table: "InboxFiles",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_InboxFiles_UploadId",
                table: "InboxFiles",
                column: "UploadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptFiles_BlobName",
                table: "ReceiptFiles",
                column: "BlobName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptFiles_ReceiptId_Position",
                table: "ReceiptFiles",
                columns: new[] { "ReceiptId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptFiles_Sha256",
                table: "ReceiptFiles",
                column: "Sha256");

            // Story #012: each receipt and each inbox item gets its file as the first file. Before the old columns go.
            migrationBuilder.Sql("""
                INSERT INTO ReceiptFiles (Id, ReceiptId, Position, BlobName, FileName, ContentType, Size, Sha256, FiledAt)
                SELECT NEWID(), Id, 1, BlobName, FileName, ContentType, Size, Sha256, FiledAt FROM Receipts;
                UPDATE Receipts SET LastFilePosition = 1;
                INSERT INTO InboxFiles (Id, InboxItemId, Position, UploadId, FileName, ContentType, Size, Sha256)
                SELECT NEWID(), Id, 1, UploadId, FileName, ContentType, Size, Sha256 FROM InboxItems;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Receipts_Sha256",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_InboxItems_Sha256",
                table: "InboxItems");

            migrationBuilder.DropIndex(
                name: "IX_InboxItems_UploadId",
                table: "InboxItems");

            migrationBuilder.DropColumn(
                name: "BlobName",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "FiledAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "InboxItems");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "InboxItems");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "InboxItems");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "InboxItems");

            migrationBuilder.DropColumn(
                name: "UploadId",
                table: "InboxItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxFiles");

            migrationBuilder.DropTable(
                name: "ReceiptFiles");

            migrationBuilder.DropColumn(
                name: "LastFilePosition",
                table: "Receipts");

            migrationBuilder.AddColumn<string>(
                name: "BlobName",
                table: "Receipts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Receipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Receipts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FiledAt",
                table: "Receipts",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Receipts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "InboxItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "InboxItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "InboxItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "InboxItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadId",
                table: "InboxItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Sha256",
                table: "Receipts",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_Sha256",
                table: "InboxItems",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_UploadId",
                table: "InboxItems",
                column: "UploadId",
                unique: true);
        }
    }
}
