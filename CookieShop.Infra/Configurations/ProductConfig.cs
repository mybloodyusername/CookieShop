using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookieShop.Infra.Configurations;

public class ProductConfig : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Product_Price", "\"Price\" >= 1 AND \"Price\" <= 999999999999 AND \"Price\" = trunc(\"Price\")");
            t.HasCheckConstraint("CK_Product_SalePrice", "\"SalePrice\" IS NULL OR (\"SalePrice\" >= 1 AND \"SalePrice\" < \"Price\" AND \"SalePrice\" = trunc(\"SalePrice\"))");
            t.HasCheckConstraint("CK_Product_SaleRequired", "NOT \"IsOnSale\" OR \"SalePrice\" IS NOT NULL");
            t.HasCheckConstraint("CK_Product_Stock", "\"StockQuantity\" >= 0");
        });
        builder.Property<uint>("Version").IsRowVersion();
        builder.Property(e => e.Price).HasPrecision(18, 2);
        builder.Property(e => e.SalePrice).HasPrecision(18, 2);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
