using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookieShop.Infra.Configurations;

public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrderItem_Price", "\"UnitPrice\" >= 1 AND \"UnitPrice\" = trunc(\"UnitPrice\")");
            t.HasCheckConstraint("CK_OrderItem_Quantity", "\"Quantity\" BETWEEN 1 AND 1000");
        });
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
