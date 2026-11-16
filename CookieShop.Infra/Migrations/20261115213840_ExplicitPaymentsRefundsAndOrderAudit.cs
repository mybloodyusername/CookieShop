using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitPaymentsRefundsAndOrderAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FinancialEntries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "FinancialEntries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OccurredAt",
                table: "FinancialEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusChanges_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusChanges_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE "PaymentAttempts" p SET "UserId" = o."UserId" FROM "Orders" o WHERE o."Id" = p."OrderId";
                UPDATE "Orders" SET "PaymentStatus" = 'WalletCredited' WHERE "PaymentStatus" = 'Refunded';
                INSERT INTO "OrderStatusChanges" ("Id", "OrderId", "FromStatus", "ToStatus", "ActorId", "Reason", "CreatedAt")
                SELECT gen_random_uuid(), "Id", NULL, "Status", NULL,
                    'Migration baseline; previous transition history unavailable', now() FROM "Orders";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_UserId",
                table: "PaymentAttempts",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_RefundedAmount",
                table: "Orders",
                sql: "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"PaidAmount\" AND \"RefundedAmount\" = trunc(\"RefundedAmount\")");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_RecordedBy",
                table: "FinancialEntries",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_UserId_CreatedAt",
                table: "FinancialEntries",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChanges_ActorId",
                table: "OrderStatusChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChanges_OrderId_CreatedAt",
                table: "OrderStatusChanges",
                columns: new[] { "OrderId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEntries_AspNetUsers_RecordedBy",
                table: "FinancialEntries",
                column: "RecordedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_AspNetUsers_UserId",
                table: "PaymentAttempts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "PaymentAttempts" WHERE "OrderId" IS NULL)
                        OR EXISTS (SELECT 1 FROM "FinancialEntries" WHERE "Kind" = 'Refund') THEN
                        RAISE EXCEPTION 'Cannot downgrade: wallet gateway attempts or recorded external refunds require this schema.';
                    END IF;
                END $$;
                UPDATE "Orders" SET "PaymentStatus" = 'Refunded' WHERE "PaymentStatus" = 'WalletCredited';
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialEntries_AspNetUsers_RecordedBy",
                table: "FinancialEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_AspNetUsers_UserId",
                table: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "OrderStatusChanges");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_UserId",
                table: "PaymentAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_RefundedAmount",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_RecordedBy",
                table: "FinancialEntries");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_UserId_CreatedAt",
                table: "FinancialEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FinancialEntries");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "FinancialEntries");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "FinancialEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
