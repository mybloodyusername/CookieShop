using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class SeparateOrderPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PayLater",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unpaid");

            migrationBuilder.AddColumn<bool>(
                name: "AllowPayLater",
                table: "CustomerPaymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPayments", x => x.Id);
                    table.CheckConstraint("CK_OrderPayment_Amount", "\"Amount\" > 0 AND \"Amount\" = trunc(\"Amount\")");
                    table.ForeignKey(
                        name: "FK_OrderPayments_FinancialEntries_FinancialEntryId",
                        column: x => x.FinancialEntryId,
                        principalTable: "FinancialEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Authority = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.CheckConstraint("CK_PaymentAttempt_Amount", "\"Amount\" > 0 AND \"Amount\" = trunc(\"Amount\")");
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_PaidAmount",
                table: "Orders",
                sql: "\"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"TotalAmount\" AND \"PaidAmount\" = trunc(\"PaidAmount\")");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_FinancialEntryId",
                table: "OrderPayments",
                column: "FinancialEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                table: "OrderPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_OrderId",
                table: "PaymentAttempts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_Provider_Authority",
                table: "PaymentAttempts",
                columns: new[] { "Provider", "Authority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_Provider_TransactionId",
                table: "PaymentAttempts",
                columns: new[] { "Provider", "TransactionId" },
                unique: true,
                filter: "\"TransactionId\" IS NOT NULL");
            // Do not guess whether historical fulfilled orders without a charge were paid.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "Orders" o WHERE o."Status" IN ('Accepted','Shipped','Delivered')
                        AND o."TotalAmount" > 0 AND NOT EXISTS
                        (SELECT 1 FROM "FinancialEntries" f WHERE f."OrderId" = o."Id" AND f."Kind" = 'OrderCharge')) THEN
                        RAISE EXCEPTION 'Reconcile legacy fulfilled orders without OrderCharge before migrating order payments (or explicitly reset development samples).';
                    END IF;
                END $$;
                UPDATE "Orders" SET "PaymentStatus" = 'Unpaid';
                -- The old Paid state is explicit evidence of payment. Preserve its net account balance.
                INSERT INTO "FinancialEntries" ("Id","UserId","OrderId","Amount","DueAt","Kind","Reference","RecordedBy","CreatedAt")
                SELECT gen_random_uuid(), o."UserId", o."Id", -o."TotalAmount", NULL, 'OrderPrepayment', '', NULL, o."CreatedAt"
                FROM "Orders" o WHERE o."Status" = 'Paid' AND o."TotalAmount" > 0 AND NOT EXISTS
                    (SELECT 1 FROM "FinancialEntries" f WHERE f."OrderId" = o."Id" AND f."Kind" = 'OrderCharge');
                INSERT INTO "FinancialEntries" ("Id","UserId","OrderId","Amount","DueAt","Kind","Reference","RecordedBy","CreatedAt")
                SELECT gen_random_uuid(), o."UserId", o."Id", o."TotalAmount", NULL, 'LegacyPayment', 'legacy:' || o."Id", NULL, o."CreatedAt"
                FROM "Orders" o WHERE o."Status" = 'Paid' AND o."TotalAmount" > 0 AND NOT EXISTS
                    (SELECT 1 FROM "FinancialEntries" f WHERE f."OrderId" = o."Id" AND f."Kind" = 'OrderCharge');
                -- Allocate the old pooled funds to live charges oldest first, preserving ledger totals.
                WITH charges AS (
                    SELECT o."Id", o."UserId", o."TotalAmount", o."CreatedAt",
                        SUM(o."TotalAmount") OVER (PARTITION BY o."UserId") AS total,
                        COALESCE(SUM(o."TotalAmount") OVER (PARTITION BY o."UserId" ORDER BY o."CreatedAt", o."Id"
                            ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) AS preceding
                    FROM "Orders" o WHERE o."Status" IN ('Accepted','Shipped','Delivered','Paid') AND EXISTS
                        (SELECT 1 FROM "FinancialEntries" f WHERE f."OrderId" = o."Id" AND f."Kind" = 'OrderCharge')
                ), allocations AS (
                    SELECT c.*, LEAST(c."TotalAmount", GREATEST(0, c.total +
                        COALESCE((SELECT SUM(f."Amount") FROM "FinancialEntries" f WHERE f."UserId" = c."UserId"),0) - c.preceding)) AS paid
                    FROM charges c
                )
                UPDATE "Orders" o SET "PaidAmount" = a.paid, "PayLater" = true
                FROM allocations a WHERE o."Id" = a."Id";
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "Orders" o WHERE o."Status" = 'Paid' AND o."PaidAmount" < o."TotalAmount"
                        AND EXISTS (SELECT 1 FROM "FinancialEntries" f WHERE f."OrderId" = o."Id" AND f."Kind" = 'OrderCharge')) THEN
                        RAISE EXCEPTION 'Reconcile legacy Paid orders whose financial ledger has insufficient funds before migrating.';
                    END IF;
                END $$;
                UPDATE "Orders" SET "PaidAmount" = "TotalAmount", "Status" = 'Accepted' WHERE "Status" = 'Paid';
                INSERT INTO "OrderPayments" ("Id","OrderId","FinancialEntryId","Amount","Source","CreatedAt")
                SELECT gen_random_uuid(), "Id", NULL, "PaidAmount", 'LegacyAllocation', "CreatedAt"
                FROM "Orders" WHERE "PaidAmount" > 0;
                UPDATE "Orders" SET "PaymentStatus" = CASE WHEN "PaidAmount" >= "TotalAmount" THEN 'Paid'
                    WHEN "PaidAmount" > 0 THEN 'PartiallyPaid' ELSE 'Unpaid' END;
                -- Eligibility remains disabled until an administrator explicitly enables it.
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPayments");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_PaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayLater",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AllowPayLater",
                table: "CustomerPaymentTerms");
        }
    }
}
