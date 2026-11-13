using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPaymentTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                table: "FinancialEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerPaymentTerms",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyDueDay = table.Column<int>(type: "integer", nullable: true),
                    DebtLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentTerms", x => x.UserId);
                    table.CheckConstraint("CK_PaymentTerms_Limit", "\"DebtLimit\" IS NULL OR (\"DebtLimit\" >= 0 AND \"DebtLimit\" = trunc(\"DebtLimit\"))");
                    table.ForeignKey(
                        name: "FK_CustomerPaymentTerms_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPaymentTerms");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "FinancialEntries");
        }
    }
}
