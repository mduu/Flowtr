using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Flowtr.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClosedYears",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosedYears", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "Counterparties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counterparties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AmountChf = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OriginalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Counterparties_CounterpartyId",
                        column: x => x.CounterpartyId,
                        principalTable: "Counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsActive", "Kind", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0199d6a0-0001-7000-8000-000000000001"), true, "Income", "Umsatz und Honorare", 10 },
                    { new Guid("0199d6a0-0001-7000-8000-000000000002"), true, "Income", "Übrige Erträge", 20 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000001"), true, "Expense", "Material und Waren", 10 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000002"), true, "Expense", "Fremdleistungen", 20 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000003"), true, "Expense", "Software und Abos", 30 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000004"), true, "Expense", "Hardware und Geräte", 40 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000005"), true, "Expense", "Raumkosten", 50 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000006"), true, "Expense", "Fahrzeug und Reisen", 60 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000007"), true, "Expense", "Telefon und Internet", 70 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000008"), true, "Expense", "Werbung und Marketing", 80 },
                    { new Guid("0199d6a0-0002-7000-8000-000000000009"), true, "Expense", "Weiterbildung und Fachliteratur", 90 },
                    { new Guid("0199d6a0-0002-7000-8000-00000000000a"), true, "Expense", "Versicherungen und Gebühren", 100 },
                    { new Guid("0199d6a0-0002-7000-8000-00000000000b"), true, "Expense", "Bank- und Kreditkartenspesen", 110 },
                    { new Guid("0199d6a0-0002-7000-8000-00000000000c"), true, "Expense", "Übriger Aufwand", 120 }
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("0199d6a0-0003-7000-8000-000000000001"), "Bankkonto" },
                    { new Guid("0199d6a0-0003-7000-8000-000000000002"), "Kreditkarte" },
                    { new Guid("0199d6a0-0003-7000-8000-000000000003"), "Bar" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CategoryId",
                table: "Bookings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CounterpartyId",
                table: "Bookings",
                column: "CounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Date",
                table: "Bookings",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentMethodId",
                table: "Bookings",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Kind_Name",
                table: "Categories",
                columns: new[] { "Kind", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_Name",
                table: "Counterparties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Name",
                table: "PaymentMethods",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "ClosedYears");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Counterparties");

            migrationBuilder.DropTable(
                name: "PaymentMethods");
        }
    }
}
