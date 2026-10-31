using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flowtr.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllowedSenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedSenders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportedMails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Sender = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OriginalSender = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedMails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Analysis = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnalysisError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Proposal = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboxItems_ImportedMails_MailId",
                        column: x => x.MailId,
                        principalTable: "ImportedMails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Sha256",
                table: "Receipts",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedSenders_Address",
                table: "AllowedSenders",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedMails_MessageKey",
                table: "ImportedMails",
                column: "MessageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_MailId",
                table: "InboxItems",
                column: "MailId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowedSenders");

            migrationBuilder.DropTable(
                name: "InboxItems");

            migrationBuilder.DropTable(
                name: "ImportedMails");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_Sha256",
                table: "Receipts");
        }
    }
}
