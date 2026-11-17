using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MonthlyBillingAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_OrderId",
                table: "FinancialEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentTerms_Limit",
                table: "CustomerPaymentTerms");

            migrationBuilder.RenameColumn(
                name: "DebtLimit",
                table: "CustomerPaymentTerms",
                newName: "MonthlySpendingLimit");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlySpendingLimit",
                table: "CustomerPaymentTerms",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.Sql("UPDATE \"CustomerPaymentTerms\" SET \"MonthlySpendingLimit\" = 0 WHERE NOT \"AllowPayLater\";");
            migrationBuilder.DropColumn(name: "AllowPayLater", table: "CustomerPaymentTerms");
            migrationBuilder.Sql("UPDATE \"FinancialEntries\" SET \"Kind\" = 'MonthlyCharge' WHERE \"Kind\" = 'OrderCharge';");

            migrationBuilder.RenameColumn(
                name: "PayLater",
                table: "Orders",
                newName: "MonthlyBilling");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_OrderId",
                table: "FinancialEntries",
                column: "OrderId",
                unique: true,
                filter: "\"Kind\" = 'MonthlyCharge'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentTerms_MonthlySpendingLimit",
                table: "CustomerPaymentTerms",
                sql: "\"MonthlySpendingLimit\" IS NULL OR (\"MonthlySpendingLimit\" >= 0 AND \"MonthlySpendingLimit\" = trunc(\"MonthlySpendingLimit\"))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_OrderId",
                table: "FinancialEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentTerms_MonthlySpendingLimit",
                table: "CustomerPaymentTerms");

            migrationBuilder.Sql("UPDATE \"FinancialEntries\" SET \"Kind\" = 'OrderCharge' WHERE \"Kind\" = 'MonthlyCharge';");

            migrationBuilder.AddColumn<decimal>(
                name: "DebtLimit",
                table: "CustomerPaymentTerms",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPayLater",
                table: "CustomerPaymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"CustomerPaymentTerms\" SET \"AllowPayLater\" = \"MonthlySpendingLimit\" IS NULL OR \"MonthlySpendingLimit\" > 0, \"DebtLimit\" = CASE WHEN \"MonthlySpendingLimit\" > 0 THEN \"MonthlySpendingLimit\" ELSE NULL END;");

            migrationBuilder.DropColumn(name: "MonthlySpendingLimit", table: "CustomerPaymentTerms");

            migrationBuilder.RenameColumn(
                name: "MonthlyBilling",
                table: "Orders",
                newName: "PayLater");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_OrderId",
                table: "FinancialEntries",
                column: "OrderId",
                unique: true,
                filter: "\"Kind\" = 'OrderCharge'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentTerms_Limit",
                table: "CustomerPaymentTerms",
                sql: "\"DebtLimit\" IS NULL OR (\"DebtLimit\" >= 0 AND \"DebtLimit\" = trunc(\"DebtLimit\"))");
        }
    }
}
