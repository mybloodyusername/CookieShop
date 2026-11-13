using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CreateFinancialEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEntries", x => x.Id);
                    table.CheckConstraint("CK_FinancialEntry_Amount", "\"Amount\" <> 0 AND \"Amount\" = trunc(\"Amount\")");
                    table.ForeignKey(
                        name: "FK_FinancialEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_OrderId",
                table: "FinancialEntries",
                column: "OrderId",
                unique: true,
                filter: "\"Kind\" = 'OrderCharge'");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_UserId_Reference",
                table: "FinancialEntries",
                columns: new[] { "UserId", "Reference" },
                unique: true,
                filter: "\"Reference\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialEntries");
        }
    }
}
