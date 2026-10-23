using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookieShop.Infra.Migrations
{
    /// <inheritdoc />
    public partial class StoreSecurityAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine1",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine2",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                table: "Orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingTitle",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CartItems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            // Existing orders retain the delivery details available at migration time.
            migrationBuilder.Sql("""
                UPDATE "Orders" AS o SET
                    "ShippingTitle" = a."Title",
                    "ShippingAddressLine1" = a."AddressLine1",
                    "ShippingAddressLine2" = a."AddressLine2",
                    "ShippingPhoneNumber" = a."PhoneNumber"
                FROM "Addresses" AS a WHERE a."Id" = o."AddressId";
                """);

            migrationBuilder.CreateTable(
                name: "MediaImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Alt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Album = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StorageProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ByteLength = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageBlobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBlobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageBlobs_MediaImages_Id",
                        column: x => x.Id,
                        principalTable: "MediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => new { x.ProductId, x.ImageId });
                    table.ForeignKey(
                        name: "FK_ProductImages_MediaImages_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Price",
                table: "Products",
                sql: "\"Price\" >= 1 AND \"Price\" <= 999999999999 AND \"Price\" = trunc(\"Price\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_SalePrice",
                table: "Products",
                sql: "\"SalePrice\" IS NULL OR (\"SalePrice\" >= 1 AND \"SalePrice\" < \"Price\" AND \"SalePrice\" = trunc(\"SalePrice\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_SaleRequired",
                table: "Products",
                sql: "NOT \"IsOnSale\" OR \"SalePrice\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Stock",
                table: "Products",
                sql: "\"StockQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Total",
                table: "Orders",
                sql: "\"TotalAmount\" >= 0 AND \"TotalAmount\" = trunc(\"TotalAmount\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_Price",
                table: "OrderItems",
                sql: "\"UnitPrice\" >= 1 AND \"UnitPrice\" = trunc(\"UnitPrice\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_Quantity",
                table: "OrderItems",
                sql: "\"Quantity\" BETWEEN 1 AND 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItem_Quantity",
                table: "CartItems",
                sql: "\"Quantity\" BETWEEN 1 AND 1000");

            migrationBuilder.CreateIndex(
                name: "IX_MediaImages_Album",
                table: "MediaImages",
                column: "Album");

            migrationBuilder.CreateIndex(
                name: "IX_MediaImages_CreatedAt_Id",
                table: "MediaImages",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ImageId",
                table: "ProductImages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SortOrder",
                table: "ProductImages",
                columns: new[] { "ProductId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageBlobs");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "MediaImages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Price",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_SalePrice",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_SaleRequired",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Stock",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Total",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_Price",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_Quantity",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItem_Quantity",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingAddressLine1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressLine2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingTitle",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CartItems");
        }
    }
}
