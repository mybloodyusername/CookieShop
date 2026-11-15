using Microsoft.EntityFrameworkCore.Migrations;
using System.Globalization;
using System.Text;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            // Use the same .NET Persian calendar as checkout, including its leap-year
            // rules. An indexed, temporary year-boundary table avoids a second calendar
            // implementation in PostgreSQL and supports historical Tehran offsets.
            var calendar = new PersianCalendar();
            var starts = new StringBuilder("CREATE TEMP TABLE order_number_years (starts date PRIMARY KEY, year integer NOT NULL) ON COMMIT DROP; INSERT INTO order_number_years VALUES ");
            for (var year = 1; year <= calendar.GetYear(calendar.MaxSupportedDateTime); year++)
            {
                if (year > 1) starts.Append(',');
                var start = calendar.ToDateTime(year, 1, 1, 0, 0, 0, 0);
                starts.Append(CultureInfo.InvariantCulture, $"('{start:yyyy-MM-dd}',{year})");
            }
            starts.Append(';');
            migrationBuilder.Sql(starts.ToString());
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    item record;
                    candidate text;
                    attempt integer;
                BEGIN
                    FOR item IN
                        SELECT o."Id", y.year,
                            (o."CreatedAt" AT TIME ZONE 'Asia/Tehran')::date - y.starts AS day
                        FROM "Orders" o
                        CROSS JOIN LATERAL (
                            SELECT starts, year FROM order_number_years
                            WHERE starts <= (o."CreatedAt" AT TIME ZONE 'Asia/Tehran')::date
                            ORDER BY starts DESC LIMIT 1
                        ) y
                    LOOP
                        FOR attempt IN 1..100 LOOP
                            candidate := lpad(item.year::text, 4, '0') ||
                                lpad((CASE WHEN item.day < 186 THEN item.day / 31 + 1
                                    ELSE (item.day - 186) / 30 + 7 END)::text, 2, '0') ||
                                lpad((CASE WHEN item.day < 186 THEN item.day % 31 + 1
                                    ELSE (item.day - 186) % 30 + 1 END)::text, 2, '0') || '-' ||
                                lpad(((('x' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))::bit(32)::bigint) % 1000000)::text, 6, '0');
                            BEGIN
                                UPDATE "Orders" SET "OrderNumber" = candidate WHERE "Id" = item."Id";
                                EXIT;
                            EXCEPTION WHEN unique_violation THEN
                                IF attempt = 100 THEN
                                    RAISE EXCEPTION 'Could not allocate a unique historical order number.';
                                END IF;
                            END;
                        END LOOP;
                    END LOOP;
                END $$;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber", table: "Orders", type: "character varying(15)",
                maxLength: 15, nullable: false, oldClrType: typeof(string),
                oldType: "character varying(15)", oldMaxLength: 15, oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Number",
                table: "Orders",
                sql: "\"OrderNumber\" ~ '^[0-9]{8}-[0-9]{6}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Number",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");
        }
    }
}
